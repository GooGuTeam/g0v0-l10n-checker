// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N009 - a base resource that no localisation class claims, reported once per file.
/// This is the file-level counterpart of <see cref="OrphanResourceKeyChecker"/>; the per-key form is
/// usually more useful, so this only fires for files that matched nothing whatsoever.
/// </summary>
internal sealed class OrphanResxFileChecker : IChecker
{
    public string RuleId => RuleIds.OrphanResxFile;

    public Severity DefaultSeverity => Severity.Warning;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (LocalisationClass cls in context.Workspace.Classes)
        {
            ResourceFile? resource = context.Matches[cls].Effective;
            if (resource is not null)
                claimed.Add(resource.AbsolutePath);
        }

        foreach (ResourceFile resource in context.Workspace.Resources)
        {
            if (!resource.InReverseScope || claimed.Contains(resource.AbsolutePath))
                continue;

            yield return new Diagnostic(
                RuleId,
                severity,
                resource.Source,
                resource.DisplayPath,
                0,
                $"resource file matches no localisation class ({resource.Entries.Count} keys)",
                Key: resource.ResourceName);
        }
    }
}
