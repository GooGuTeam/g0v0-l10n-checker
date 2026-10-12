// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// A single rule violation. <see cref="File"/> is always repository-relative (forward slashes),
/// which is what both the CI annotation format and the baseline file key on.
/// </summary>
internal sealed record Diagnostic(
    string RuleId,
    Severity Severity,
    DiagnosticSource Source,
    string File,
    int Line,
    string Message,
    string? Key = null,
    string? Suggestion = null)
{
    /// <summary>A stable identity used to match this diagnostic against a baseline entry.</summary>
    public string BaselineKey => BuildKey(RuleId, Source, File, Key);

    /// <summary>Builds the same identity the baseline file stores, so the two can never drift.</summary>
    public static string BuildKey(string ruleId, DiagnosticSource source, string file, string? key)
        => $"{ruleId}|{source}|{file}|{key}";
}
