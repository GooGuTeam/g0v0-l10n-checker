// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// Which of the scanned roots a diagnostic originated from.
/// </summary>
internal enum DiagnosticSource
{
    /// <summary>The osu! game repository (the <c>*Strings.cs</c> side).</summary>
    Osu,

    /// <summary>The osu-resources repository (the <c>.resx</c> side).</summary>
    Resources,

    /// <summary>An additional resx folder supplied via <c>--extra-resx</c>.</summary>
    Extra,
}
