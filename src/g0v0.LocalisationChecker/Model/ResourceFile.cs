// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// A parsed base (culture-neutral) resx file.
/// </summary>
internal sealed class ResourceFile
{
    /// <summary>Absolute path on disk.</summary>
    public required string AbsolutePath { get; init; }

    /// <summary>Repository-relative path with forward slashes, used for display and baselines.</summary>
    public required string DisplayPath { get; init; }

    /// <summary>Which scan root this file came from.</summary>
    public required DiagnosticSource Source { get; init; }

    /// <summary>
    /// The manifest resource name derived from the file's path, e.g.
    /// <c>osu.Game.Resources.Localisation.HUD.SpectatorList</c>. This is exactly what the
    /// runtime <c>ResourceManager</c> lookup uses, so it is the authoritative identity.
    /// </summary>
    public required string ResourceName { get; init; }

    /// <summary>
    /// Path of the containing directory relative to the localisation root, e.g. <c>HUD</c> or <c>""</c>.
    /// Used as a disambiguation hint when a class's <c>prefix</c> was written incorrectly.
    /// </summary>
    public required string RelativeDirectory { get; init; }

    /// <summary>
    /// Whether this file participates in the reverse ("resx -&gt; class member") checks.
    /// Deliberately false for <c>Web/**</c> and for <c>--extra-resx</c> folders.
    /// </summary>
    public required bool InReverseScope { get; init; }

    /// <summary>Culture-neutral entries.</summary>
    public required IReadOnlyList<ResourceEntry> Entries { get; init; }

    private IReadOnlyDictionary<string, ResourceEntry>? entriesByName;

    /// <summary>Entries keyed by <c>name</c>; first occurrence wins if a file repeats a key.</summary>
    public IReadOnlyDictionary<string, ResourceEntry> EntriesByName
        => entriesByName ??= Entries
            .GroupBy(e => e.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    public bool HasKey(string key) => EntriesByName.ContainsKey(key);
}
