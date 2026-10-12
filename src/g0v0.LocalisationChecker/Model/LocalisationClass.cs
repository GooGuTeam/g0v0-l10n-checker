// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// A parsed <c>*Strings.cs</c> localisation class. The <see cref="Prefix"/> constant is the only
/// authoritative source of this class's resource namespace; member names and file names are not.
/// </summary>
internal sealed class LocalisationClass
{
    public required string AbsolutePath { get; init; }

    /// <summary>Repository-relative path with forward slashes, used for display and baselines.</summary>
    public required string DisplayPath { get; init; }

    public required string ClassName { get; init; }

    /// <summary>Value of the <c>private const string prefix</c> field.</summary>
    public required string Prefix { get; init; }

    /// <summary>1-based line of the <c>prefix</c> declaration.</summary>
    public required int PrefixLine { get; init; }

    /// <summary>
    /// Path of the containing directory relative to the localisation root, e.g. <c>HUD</c> or <c>""</c>.
    /// </summary>
    public required string RelativeDirectory { get; init; }

    public required IReadOnlyList<LocalisationMember> Members { get; init; }

    /// <summary>Resource whose derived name equals <see cref="Prefix"/>, if any.</summary>
    public ResourceFile? ExactResource { get; set; }

    /// <summary>Resource matched heuristically when the prefix was written incorrectly.</summary>
    public ResourceFile? SuggestedResource { get; set; }

    /// <summary>True when several resources were plausible and none won decisively.</summary>
    public bool MatchAmbiguous { get; set; }

    /// <summary>Candidate resources considered when the exact match failed.</summary>
    public IReadOnlyList<ResourceFile> MatchCandidates { get; set; } = [];

    /// <summary>The resource used for key checks.</summary>
    public ResourceFile? EffectiveResource => ExactResource ?? SuggestedResource;

    /// <summary>All keys declared by this class's members.</summary>
    public IEnumerable<string> Keys
        => Members.Where(m => m.Key is not null).Select(m => m.Key!);
}
