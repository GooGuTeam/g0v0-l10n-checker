// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N010 - two scanned files derive the same resource name. Because the SDK builds the name from
/// the path, an accidental duplicate means one of the two files silently shadows the other at
/// runtime.
/// </summary>
internal sealed class DuplicateResourceNameChecker : IChecker
{
    public string RuleId => RuleIds.DuplicateResourceName;

    public Severity DefaultSeverity => Severity.Warning;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        IEnumerable<IGrouping<string, ResourceFile>> groups = context.Workspace.Resources
            .GroupBy(resource => resource.ResourceName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (IGrouping<string, ResourceFile> group in groups)
        {
            // The first file wins in LocalisationMatcher, so everything after it is the shadowed one.
            foreach (ResourceFile duplicate in group.Skip(1))
            {
                yield return new Diagnostic(
                    RuleId,
                    severity,
                    duplicate.Source,
                    duplicate.DisplayPath,
                    0,
                    $"resource name '{duplicate.ResourceName}' is already provided by another file",
                    Key: duplicate.ResourceName);
            }
        }
    }
}
