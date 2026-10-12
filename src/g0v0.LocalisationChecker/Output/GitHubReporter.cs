// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Checking;
using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Output;

/// <summary>
/// Emits GitHub Actions workflow commands, which surface as inline annotations on the PR diff.
/// </summary>
/// <remarks>
/// The escaping rules come from the Actions toolkit: <c>%</c>, CR and LF must be escaped in the
/// message body, and property values additionally need <c>:</c> and <c>,</c> escaped since those are
/// the separators of the property list.
/// </remarks>
internal sealed class GitHubReporter : IReporter
{
    public void Report(IReadOnlyList<Diagnostic> diagnostics, ReportSummary summary, TextWriter writer)
    {
        foreach (Diagnostic diagnostic in diagnostics)
        {
            string level = diagnostic.Severity switch
            {
                Severity.Error => "error",
                Severity.Warning => "warning",
                _ => "notice",
            };

            string properties = $"file={EscapeProperty(diagnostic.File)}";

            if (diagnostic.Line > 0)
                properties += $",line={diagnostic.Line}";

            properties += $",title={EscapeProperty(RuleIds.Title(diagnostic.RuleId))}";

            writer.WriteLine($"::{level} {properties}::{EscapeData(diagnostic.Message)}");
        }

        writer.WriteLine($"::notice title=localisation-checker::Checked {summary.Classes} classes / "
                         + $"{summary.Resources} resources - {summary.Errors} error(s), "
                         + $"{summary.Warnings} warning(s), {summary.Infos} info(s)"
                         + (summary.Suppressed > 0 ? $", {summary.Suppressed} suppressed" : string.Empty));
    }

    private static string EscapeData(string value)
        => value.Replace("%", "%25", StringComparison.Ordinal)
            .Replace("\r", "%0D", StringComparison.Ordinal)
            .Replace("\n", "%0A", StringComparison.Ordinal);

    private static string EscapeProperty(string value)
        => EscapeData(value)
            .Replace(":", "%3A", StringComparison.Ordinal)
            .Replace(",", "%2C", StringComparison.Ordinal);
}
