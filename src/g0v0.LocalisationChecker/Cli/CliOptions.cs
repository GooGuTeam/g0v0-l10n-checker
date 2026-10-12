// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Output;

namespace g0v0.LocalisationChecker.Cli;

internal enum OutputFormat
{
    Auto,
    Human,
    GitHub,
    Json,
}

internal enum FailOn
{
    Never,
    Error,
    Warning,
}

/// <summary>
/// Everything the command line can control. Defaults are tuned so that running with no arguments
/// works against the standard local checkout layout.
/// </summary>
internal sealed class CliOptions
{
    /// <summary>Convenience default for a local development machine; CI must pass this explicitly.</summary>
    public const string DefaultOsuRoot = "D:/Projects/osu";

    public const string DefaultResourcesRoot = "D:/Projects/osu-resources";

    /// <summary>Name of the baseline file picked up automatically from the working directory.</summary>
    public const string DefaultBaselineFileName = ".localisation-baseline.json";

    public string OsuRoot { get; set; } =
        Environment.GetEnvironmentVariable("G0V0_OSU_PATH") ?? DefaultOsuRoot;

    public string ResourcesRoot { get; set; } =
        Environment.GetEnvironmentVariable("G0V0_RESOURCES_PATH") ?? DefaultResourcesRoot;

    public List<ExtraResxFolder> ExtraResx { get; } = [];

    public ColorMode Color { get; set; } = ColorMode.Auto;

    public OutputFormat Format { get; set; } = OutputFormat.Auto;

    public Severity MinSeverity { get; set; } = Severity.Info;

    public FailOn FailOn { get; set; } = FailOn.Error;

    public List<string> IgnoreRules { get; } = [];

    /// <summary>Explicit baseline path. Null means "look in the working directory, if present".</summary>
    public string? BaselinePath { get; set; }

    /// <summary>Disables baseline handling entirely.</summary>
    public bool NoBaseline { get; set; }

    /// <summary>When set, the current diagnostics are written here instead of being filtered.</summary>
    public string? WriteBaselinePath { get; set; }

    /// <summary>
    /// When set, the JSON document is also written here regardless of <see cref="Format"/>, so CI can
    /// have annotations on stdout and machine-readable totals in a file at the same time.
    /// </summary>
    public string? JsonReportPath { get; set; }

    /// <summary>
    /// When set, a SARIF report for <see cref="SarifSource"/> is written here regardless of
    /// <see cref="Format"/>, ready to be handed to GitHub code scanning.
    /// </summary>
    public string? SarifReportPath { get; set; }

    /// <summary>
    /// Which side of the cross-check the SARIF report describes. A SARIF file addresses files by a
    /// path relative to the root of the repository being scanned, so a report can only ever cover one
    /// of the two repositories.
    /// </summary>
    public DiagnosticSource SarifSource { get; set; } = DiagnosticSource.Osu;

    public bool NoSummary { get; set; }

    public bool ShowHelp { get; set; }

    public bool ShowVersion { get; set; }

    public Dictionary<string, Severity> SeverityOverrides { get; } = new(StringComparer.Ordinal);
}
