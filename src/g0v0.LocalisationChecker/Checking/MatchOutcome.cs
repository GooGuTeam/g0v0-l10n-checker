// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking;

/// <summary>How a localisation class was tied to a resource file.</summary>
internal sealed class MatchOutcome
{
    /// <summary>Resource whose derived name equals the class prefix. The happy path.</summary>
    public required ResourceFile? Exact { get; init; }

    /// <summary>A single resource the class most likely meant, when the prefix was written wrong.</summary>
    public required ResourceFile? Suggested { get; init; }

    /// <summary>Several resources were plausible and none won decisively.</summary>
    public required bool Ambiguous { get; init; }

    /// <summary>
    /// Set when the prefix does name a real resource, but none of the class's keys live there while
    /// a sibling resource defines every one of them. In practice this means the prefix was
    /// copy-pasted from a neighbouring class.
    /// </summary>
    public ResourceFile? Suspect { get; init; }

    /// <summary>Candidates considered when the exact match failed.</summary>
    public required IReadOnlyList<ResourceFile> Candidates { get; init; }

    /// <summary>The resource the class is checked against.</summary>
    public ResourceFile? Effective => Exact ?? Suggested;
}
