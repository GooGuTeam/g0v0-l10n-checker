// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Parsing;

/// <summary>
/// A string literal found in C# source. <see cref="Start"/> points at the first character of the
/// token (<c>@</c> for verbatim literals, otherwise the opening quote) and <see cref="Value"/> is
/// already unescaped.
/// </summary>
internal readonly record struct StringLiteral(int Start, int End, string Value, bool IsVerbatim);
