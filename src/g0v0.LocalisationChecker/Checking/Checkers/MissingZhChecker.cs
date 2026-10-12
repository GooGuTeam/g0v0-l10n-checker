// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N006 - every base resource should be paired with a <c>.zh.resx</c> beside it. This is a
/// repository convention of the g0v0 fork rather than an upstream requirement, which is why it is a
/// warning rather than an error.
/// </summary>
internal sealed class MissingZhChecker : IChecker
{
    public string RuleId => RuleIds.MissingZhTranslation;

    public Severity DefaultSeverity => Severity.Warning;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        foreach (ResourceFile resource in context.Workspace.Resources)
        {
            if (!resource.InReverseScope)
                continue;

            string? directory = Path.GetDirectoryName(resource.AbsolutePath);
            if (directory is null)
                continue;

            string baseName = Path.GetFileNameWithoutExtension(resource.AbsolutePath);
            string paired = Path.Combine(directory, $"{baseName}.zh.resx");

            if (File.Exists(paired))
                continue;

            yield return new Diagnostic(
                RuleId,
                severity,
                resource.Source,
                resource.DisplayPath,
                0,
                $"base resource has no paired '{baseName}.zh.resx'",
                Key: baseName);
        }
    }
}
