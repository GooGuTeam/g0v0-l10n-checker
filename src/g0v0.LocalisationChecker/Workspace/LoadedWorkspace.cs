// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Workspace;

/// <summary>
/// Everything the checkers operate on: the parsed localisation classes, the parsed base resx files,
/// and a few counters for the summary line.
/// </summary>
internal sealed class LoadedWorkspace
{
    public required string OsuRoot { get; init; }

    public required string ResourcesRoot { get; init; }

    public required IReadOnlyList<LocalisationClass> Classes { get; init; }

    public required IReadOnlyList<ResourceFile> Resources { get; init; }

    /// <summary>Number of <c>*Strings.cs</c> files that carried no <c>prefix</c> constant.</summary>
    public required int SkippedStringsFiles { get; init; }
}
