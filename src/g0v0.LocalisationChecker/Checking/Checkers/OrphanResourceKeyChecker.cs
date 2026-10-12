// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N002 - the reverse direction: a key defined in a resource that no localisation class declares.
/// Whether the string is actually rendered anywhere is irrelevant; what matters is that the
/// resource and the code have drifted apart.
/// </summary>
internal sealed class OrphanResourceKeyChecker : IChecker
{
    public string RuleId => RuleIds.OrphanResourceKey;

    public Severity DefaultSeverity => Severity.Warning;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        var declared = new HashSet<(string Resource, string Key)>();

        foreach (LocalisationClass cls in context.Workspace.Classes)
        {
            ResourceFile? resource = context.Matches[cls].Effective;
            if (resource is null)
                continue;

            foreach (string key in cls.Keys)
                declared.Add((resource.ResourceName, key));
        }

        foreach (ResourceFile resource in context.Workspace.Resources)
        {
            if (!resource.InReverseScope)
                continue;

            foreach (ResourceEntry entry in resource.Entries)
            {
                if (declared.Contains((resource.ResourceName, entry.Name)))
                    continue;

                yield return new Diagnostic(
                    RuleId,
                    severity,
                    resource.Source,
                    resource.DisplayPath,
                    entry.Line,
                    $"resource key '{entry.Name}' has no matching localisation member",
                    Key: entry.Name);
            }
        }
    }
}
