// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N001 - every key a class declares must exist in the resource it points at. A missing key
/// means the translation pipeline can never supply that string, and the English fallback baked into
/// the C# is what users see forever.
/// </summary>
internal sealed class MissingResourceChecker : IChecker
{
    public string RuleId => RuleIds.MissingResource;

    public Severity DefaultSeverity => Severity.Error;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        foreach (LocalisationClass cls in context.Workspace.Classes)
        {
            ResourceFile? resource = context.Matches[cls].Effective;
            if (resource is null)
                continue;

            foreach (LocalisationMember member in cls.Members)
            {
                if (member.Key is null || resource.HasKey(member.Key))
                    continue;

                yield return new Diagnostic(
                    RuleId,
                    severity,
                    DiagnosticSource.Osu,
                    cls.DisplayPath,
                    member.Line,
                    $"key '{member.Key}' is not defined in resource '{resource.ResourceName}'",
                    Key: member.Key);
            }
        }
    }
}
