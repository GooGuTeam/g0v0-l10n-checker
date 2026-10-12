// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.RegularExpressions;
using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N007 - resource keys should be lowercase snake_case. Dotted grouping is explicitly allowed,
/// because the repository relies on it (<c>buttons.authorise</c>).
/// </summary>
internal sealed class KeyNamingChecker : IChecker
{
    private static readonly Regex snakeCase = new(@"^[a-z0-9_]+(\.[a-z0-9_]+)*$", RegexOptions.Compiled);

    public string RuleId => RuleIds.KeyNamingViolation;

    public Severity DefaultSeverity => Severity.Warning;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        foreach (ResourceFile resource in context.Workspace.Resources)
        {
            if (!resource.InReverseScope)
                continue;

            foreach (ResourceEntry entry in resource.Entries)
            {
                if (snakeCase.IsMatch(entry.Name))
                    continue;

                yield return new Diagnostic(
                    RuleId,
                    severity,
                    resource.Source,
                    resource.DisplayPath,
                    entry.Line,
                    $"resource key '{entry.Name}' is not lowercase snake_case",
                    Key: entry.Name);
            }
        }
    }
}
