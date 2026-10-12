// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking;

/// <summary>Per-run checker configuration coming from the command line.</summary>
internal sealed class CheckerOptions
{
    /// <summary>Rules switched off entirely with <c>--ignore-rule</c>.</summary>
    public required IReadOnlySet<string> DisabledRules { get; init; }

    /// <summary>Per-rule severity overrides supplied with <c>--severity RULE=level</c>.</summary>
    public required IReadOnlyDictionary<string, Severity> SeverityOverrides { get; init; }

    public Severity SeverityFor(string ruleId, Severity fallback)
        => SeverityOverrides.TryGetValue(ruleId, out Severity severity) ? severity : fallback;
}
