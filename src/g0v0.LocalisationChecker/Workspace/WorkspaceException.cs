// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Workspace;

/// <summary>
/// Thrown when the input layout cannot be read at all (missing roots, unreadable resources).
/// Maps to exit code 3.
/// </summary>
internal sealed class WorkspaceException : Exception
{
    public WorkspaceException(string message)
        : base(message)
    {
    }
}
