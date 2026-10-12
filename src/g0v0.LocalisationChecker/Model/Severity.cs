// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// Severity of a diagnostic, ordered so that numeric comparison matches "worse than".
/// </summary>
internal enum Severity
{
    Info = 0,
    Warning = 1,
    Error = 2,
}
