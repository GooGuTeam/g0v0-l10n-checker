// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking;

/// <summary>A single rule.</summary>
internal interface IChecker
{
    /// <summary>The rule this checker implements.</summary>
    string RuleId { get; }

    /// <summary>Severity applied unless overridden on the command line.</summary>
    Severity DefaultSeverity { get; }

    IEnumerable<Diagnostic> Run(CheckContext context);
}
