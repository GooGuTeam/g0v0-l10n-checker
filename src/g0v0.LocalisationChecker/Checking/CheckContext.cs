// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Workspace;

namespace g0v0.LocalisationChecker.Checking;

/// <summary>Shared state handed to every checker.</summary>
internal sealed class CheckContext
{
    public required CheckerOptions Options { get; init; }

    public required LoadedWorkspace Workspace { get; init; }

    /// <summary>Match outcome per class, keyed by identity.</summary>
    public required IReadOnlyDictionary<LocalisationClass, MatchOutcome> Matches { get; init; }
}
