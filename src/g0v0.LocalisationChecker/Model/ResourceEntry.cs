// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// A single <c>&lt;data name="..."&gt;&lt;value&gt;...&lt;/value&gt;&lt;/data&gt;</c> entry of a resx file.
/// </summary>
internal sealed record ResourceEntry(string Name, string Value, int Line);
