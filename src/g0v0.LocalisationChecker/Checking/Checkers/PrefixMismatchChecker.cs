// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N003 - the <c>prefix</c> constant does not name a resource that exists.
/// </summary>
/// <remarks>
/// This is the highest-value rule in the tool. A wrong prefix fails silently at runtime: the
/// resource lookup misses, <c>ResourceManagerLocalisationStore</c> returns null, and the class
/// quietly serves English fallbacks forever while looking perfectly healthy. Two such cases exist
/// in the upstream repository today.
/// </remarks>
internal sealed class PrefixMismatchChecker : IChecker
{
    public string RuleId => RuleIds.PrefixMismatch;

    public Severity DefaultSeverity => Severity.Error;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        foreach (LocalisationClass cls in context.Workspace.Classes)
        {
            MatchOutcome outcome = context.Matches[cls];

            if (outcome.Exact is not null)
            {
                // The prefix is a real resource, but a sibling resource explains the class better.
                if (outcome.Suspect is not null)
                {
                    yield return new Diagnostic(
                        RuleId,
                        severity,
                        DiagnosticSource.Osu,
                        cls.DisplayPath,
                        cls.PrefixLine,
                        $"prefix '{cls.Prefix}' names '{outcome.Exact.ResourceName}', which defines none of this "
                        + $"class's keys; '{outcome.Suspect.ResourceName}' defines all of them",
                        Key: cls.Prefix,
                        Suggestion: outcome.Suspect.ResourceName);
                }

                continue;
            }

            if (outcome.Suggested is not null)
            {
                yield return new Diagnostic(
                    RuleId,
                    severity,
                    DiagnosticSource.Osu,
                    cls.DisplayPath,
                    cls.PrefixLine,
                    $"prefix '{cls.Prefix}' does not match any resource; expected '{outcome.Suggested.ResourceName}'",
                    Key: cls.Prefix,
                    Suggestion: outcome.Suggested.ResourceName);
                continue;
            }

            if (outcome.Ambiguous)
            {
                string candidates = string.Join(", ", outcome.Candidates.Select(candidate => candidate.ResourceName));

                yield return new Diagnostic(
                    RuleId,
                    severity,
                    DiagnosticSource.Osu,
                    cls.DisplayPath,
                    cls.PrefixLine,
                    $"prefix '{cls.Prefix}' does not match any resource; candidates: {candidates}",
                    Key: cls.Prefix);
            }
        }
    }
}
