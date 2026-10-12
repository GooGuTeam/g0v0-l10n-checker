// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N008 - a localisation class whose prefix names no resource at all.
/// </summary>
/// <remarks>
/// This is a warning rather than an error on purpose. At runtime the lookup is wrapped in a
/// <c>catch (MissingManifestResourceException)</c> which returns null and falls back to the C#
/// text, so a "code only" class is a supported state rather than a bug. It is still worth knowing
/// about, because every string in such a class is permanently untranslatable.
/// </remarks>
internal sealed class PureCodeClassChecker : IChecker
{
    public string RuleId => RuleIds.PureCodeClass;

    public Severity DefaultSeverity => Severity.Warning;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        foreach (LocalisationClass cls in context.Workspace.Classes)
        {
            MatchOutcome outcome = context.Matches[cls];

            if (outcome.Exact is not null || outcome.Suggested is not null || outcome.Ambiguous)
                continue;

            yield return new Diagnostic(
                RuleId,
                severity,
                DiagnosticSource.Osu,
                cls.DisplayPath,
                cls.PrefixLine,
                $"no resource found for prefix '{cls.Prefix}' - the class is not translatable",
                Key: cls.Prefix);
        }
    }
}
