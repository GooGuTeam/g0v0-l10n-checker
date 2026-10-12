// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking;

/// <summary>Small formatting helpers shared by checkers so messages stay single-line.</summary>
internal static class CheckerText
{
    private const int max_snippet = 60;

    /// <summary>Collapses CRLF/LF so two texts can be compared independently of line endings.</summary>
    public static string NormaliseNewlines(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    /// <summary>Escapes and truncates a value for inclusion in a single-line message.</summary>
    public static string Snippet(string value)
    {
        string escaped = NormaliseNewlines(value).Replace("\n", "\\n", StringComparison.Ordinal);

        return escaped.Length <= max_snippet
            ? escaped
            : string.Concat(escaped.AsSpan(0, max_snippet), "...");
    }
}
