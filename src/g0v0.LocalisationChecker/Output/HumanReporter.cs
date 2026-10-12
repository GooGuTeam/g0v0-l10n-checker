// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Output;

/// <summary>
/// Default reporter. Emits the <c>path:line: severity RULE: message</c> shape used by the
/// repository's existing <c>CheckSanity</c> scripts, so an existing CI step can consume it verbatim
/// with <c>while read -r line; do echo "::error::$line"; done</c>.
/// </summary>
internal sealed class HumanReporter : IReporter
{
    private readonly Ansi ansi;
    private readonly bool showSummary;

    public HumanReporter(Ansi ansi, bool showSummary)
    {
        this.ansi = ansi;
        this.showSummary = showSummary;
    }

    public void Report(IReadOnlyList<Diagnostic> diagnostics, ReportSummary summary, TextWriter writer)
    {
        foreach (Diagnostic diagnostic in diagnostics)
            writer.WriteLine(Format(diagnostic));

        if (!showSummary)
            return;

        writer.WriteLine();
        writer.WriteLine($"Checked {summary.Classes} localisation classes and {summary.Resources} resource files"
                         + (summary.SkippedStringsFiles > 0 ? $" ({summary.SkippedStringsFiles} *Strings.cs skipped)" : string.Empty)
                         + ".");

        writer.WriteLine($"  {ansi.ForSeverity(Severity.Error, "error")}:   {summary.Errors}");
        writer.WriteLine($"  {ansi.ForSeverity(Severity.Warning, "warning")}: {summary.Warnings}");
        writer.WriteLine($"  {ansi.ForSeverity(Severity.Info, "info")}:    {summary.Infos}");

        if (summary.Suppressed > 0)
            writer.WriteLine($"  {ansi.Dim($"suppressed by baseline: {summary.Suppressed}")}");
    }

    private string Format(Diagnostic diagnostic)
    {
        string location = diagnostic.Line > 0 ? $"{diagnostic.File}:{diagnostic.Line}" : diagnostic.File;
        string severity = diagnostic.Severity.ToString().ToLowerInvariant();

        return $"{ansi.Path(location)}: {ansi.ForSeverity(diagnostic.Severity, severity)} "
               + $"{ansi.Dim(diagnostic.RuleId)}: {diagnostic.Message}";
    }
}
