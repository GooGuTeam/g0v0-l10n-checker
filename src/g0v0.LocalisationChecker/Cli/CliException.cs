// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Cli;

/// <summary>Thrown for anything wrong with the command line itself. Maps to exit code 2.</summary>
internal sealed class CliException : Exception
{
    public CliException(string message)
        : base(message)
    {
    }
}
