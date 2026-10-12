// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Xml;
using g0v0.LocalisationChecker.Checking;
using g0v0.LocalisationChecker.Cli;
using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Output;
using g0v0.LocalisationChecker.Workspace;

namespace g0v0.LocalisationChecker;

internal static class Program
{
    public static int Main(string[] args)
    {
        CliOptions options;

        try
        {
            options = CliParser.Parse(args);
        }
        catch (CliException ex)
        {
            writeError(ex.Message);
            Console.Error.WriteLine($"Try '{VersionInfo.Product} --help' for usage.");
            return 2;
        }

        if (options.ShowHelp)
        {
            Console.Out.Write(CliHelp.Text);
            Console.Out.WriteLine();
            return 0;
        }

        if (options.ShowVersion)
        {
            Console.Out.WriteLine(VersionInfo.Display);
            return 0;
        }

        LoadedWorkspace workspace;

        try
        {
            workspace = WorkspaceLoader.Load(options.OsuRoot, options.ResourcesRoot, options.ExtraResx);
        }
        catch (Exception ex) when (ex is WorkspaceException or IOException or UnauthorizedAccessException or XmlException)
        {
            writeError(ex.Message);
            return 3;
        }

        Baseline baseline = Baseline.Empty;

        if (!options.NoBaseline)
        {
            string? baselinePath = options.BaselinePath;

            if (baselinePath is null)
            {
                string candidate = Path.Combine(Directory.GetCurrentDirectory(), CliOptions.DefaultBaselineFileName);
                if (File.Exists(candidate))
                    baselinePath = candidate;
            }

            if (baselinePath is not null)
            {
                try
                {
                    baseline = Baseline.Load(baselinePath);
                }
                catch (BaselineException ex)
                {
                    writeError(ex.Message);
                    return 3;
                }
            }
        }

        IReadOnlyDictionary<LocalisationClass, MatchOutcome> matches =
            LocalisationMatcher.Match(workspace.Classes, workspace.Resources);

        var context = new CheckContext
        {
            Options = new CheckerOptions
            {
                DisabledRules = options.IgnoreRules.ToHashSet(StringComparer.Ordinal),
                SeverityOverrides = options.SeverityOverrides,
            },
            Workspace = workspace,
            Matches = matches,
        };

        var found = new List<Diagnostic>();

        foreach (IChecker checker in CheckerRegistry.All)
        {
            if (context.Options.DisabledRules.Contains(checker.RuleId))
                continue;

            found.AddRange(checker.Run(context));
        }

        if (options.WriteBaselinePath is not null)
            writeBaseline(options.WriteBaselinePath, found);

        int suppressed = 0;
        var reported = new List<Diagnostic>();

        foreach (Diagnostic diagnostic in found)
        {
            if (baseline.Contains(diagnostic))
            {
                suppressed++;
                continue;
            }

            if (diagnostic.Severity < options.MinSeverity)
                continue;

            reported.Add(diagnostic);
        }

        reported.Sort(static (left, right) =>
        {
            int bySeverity = right.Severity.CompareTo(left.Severity);
            if (bySeverity != 0)
                return bySeverity;

            int byFile = string.CompareOrdinal(left.File, right.File);
            if (byFile != 0)
                return byFile;

            int byLine = left.Line.CompareTo(right.Line);
            if (byLine != 0)
                return byLine;

            return string.CompareOrdinal(left.RuleId, right.RuleId);
        });

        var summary = new ReportSummary
        {
            Classes = workspace.Classes.Count,
            Resources = workspace.Resources.Count,
            SkippedStringsFiles = workspace.SkippedStringsFiles,
            Errors = reported.Count(d => d.Severity == Severity.Error),
            Warnings = reported.Count(d => d.Severity == Severity.Warning),
            Infos = reported.Count(d => d.Severity == Severity.Info),
            Suppressed = suppressed,
        };

        if (options.JsonReportPath is not null)
            writeJsonReport(options.JsonReportPath, reported, summary, workspace);

        // Deliberately fed the unfiltered list: code scanning owns the alert lifecycle, so the
        // baseline keeps gating CI without hiding the known issues from the Security tab.
        if (options.SarifReportPath is not null)
            writeSarifReport(options.SarifReportPath, found, summary, options.SarifSource);

        buildReporter(options, workspace).Report(reported, summary, Console.Out);

        return hasFailed(options.FailOn, summary) ? 1 : 0;
    }

    private static void writeJsonReport(
        string path,
        IReadOnlyList<Diagnostic> diagnostics,
        ReportSummary summary,
        LoadedWorkspace workspace)
    {
        using var writer = openReport(path);
        new JsonReporter(workspace.OsuRoot, workspace.ResourcesRoot).Report(diagnostics, summary, writer);
    }

    private static void writeSarifReport(
        string path,
        IReadOnlyList<Diagnostic> diagnostics,
        ReportSummary summary,
        DiagnosticSource source)
    {
        using var writer = openReport(path);
        new SarifReporter(source).Report(diagnostics, summary, writer);
    }

    /// <summary>Creates the containing directory when needed, so a path under a fresh temp dir works.</summary>
    private static StreamWriter openReport(string path)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
            Directory.CreateDirectory(directory);

        return new StreamWriter(path);
    }

    private static void writeBaseline(string path, IReadOnlyList<Diagnostic> diagnostics)
    {
        // Only warning and above are baselined, so informational notices keep showing up.
        List<Diagnostic> entries = diagnostics.Where(d => d.Severity >= Severity.Warning).ToList();

        Baseline.Save(path, entries);

        writeError($"baseline written to {path} ({entries.Count} entries)");
    }

    private static void writeError(string message) => Console.Error.WriteLine($"{VersionInfo.Product}: {message}");

    private static IReporter buildReporter(CliOptions options, LoadedWorkspace workspace)
    {
        OutputFormat format = options.Format;

        if (format == OutputFormat.Auto)
        {
            format = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true"
                ? OutputFormat.GitHub
                : OutputFormat.Human;
        }

        bool useColor = ColorModeResolver.UseColor(options.Color);
        Ansi ansi = useColor ? Ansi.Coloured : Ansi.Plain;

        return format switch
        {
            OutputFormat.GitHub => new GitHubReporter(!options.NoSummary),
            OutputFormat.Json => new JsonReporter(workspace.OsuRoot, workspace.ResourcesRoot),
            _ => new HumanReporter(ansi, !options.NoSummary),
        };
    }

    private static bool hasFailed(FailOn failOn, ReportSummary summary) => failOn switch
    {
        FailOn.Never => false,
        FailOn.Warning => summary.Errors > 0 || summary.Warnings > 0,
        _ => summary.Errors > 0,
    };
}
