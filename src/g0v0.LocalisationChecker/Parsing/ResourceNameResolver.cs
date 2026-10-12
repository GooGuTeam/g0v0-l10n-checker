// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Parsing;

/// <summary>
/// Derives the manifest resource name of a resx file.
/// </summary>
/// <remarks>
/// The .NET SDK turns every <c>**/*.resx</c> into an embedded resource named
/// <c>$(RootNamespace).&lt;relative path with dots&gt;</c>. Since osu-resources never sets
/// <c>LogicalName</c> or a custom resource name, this derivation matches the real
/// <c>osu.Game.Resources.dll</c> manifest exactly - which means the checker needs no build step
/// and no DLL to work out what the runtime will look up.
/// </remarks>
internal static class ResourceNameResolver
{
    public static string Resolve(string filePath, string scanRoot, string rootNamespace)
    {
        string relative = Path.GetRelativePath(scanRoot, filePath);
        string withoutExtension = relative[..^".resx".Length];
        string dotted = withoutExtension.Replace('\\', '.').Replace('/', '.');

        return rootNamespace.Length == 0 ? dotted : $"{rootNamespace}.{dotted}";
    }

    /// <summary>Path of the containing directory relative to <paramref name="scanRoot"/>, dot-free.</summary>
    public static string RelativeDirectory(string filePath, string scanRoot)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (dir is null)
            return string.Empty;

        string relative = Path.GetRelativePath(scanRoot, dir)
            .Replace('\\', '/');

        return relative == "." ? string.Empty : relative;
    }
}
