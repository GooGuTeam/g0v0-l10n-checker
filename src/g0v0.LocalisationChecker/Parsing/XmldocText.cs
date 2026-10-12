// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;

namespace g0v0.LocalisationChecker.Parsing;

/// <summary>
/// Extracts the translation text carried by a member's XML doc comment.
/// </summary>
/// <remarks>
/// The localisation analyser always emits the doc as:
/// <code>
/// /// &lt;summary&gt;
/// /// "Back"
/// /// &lt;/summary&gt;
/// </code>
/// The text is HTML-escaped (<c>&amp;#39;</c>, <c>&amp;quot;</c>) and multi-line entries keep real
/// newlines. Decoding therefore has to happen before it can be compared with the C# fallback.
/// </remarks>
internal static class XmldocText
{
    /// <summary>Returns the decoded translation text, or null when the block carries no summary.</summary>
    public static string? Extract(IReadOnlyList<string> docLines)
    {
        if (docLines.Count == 0)
            return null;

        var body = new List<string>();
        bool inSummary = false;

        foreach (string raw in docLines)
        {
            string line = raw.TrimStart();

            if (!line.StartsWith("///", StringComparison.Ordinal))
                continue;

            line = line[3..];
            if (line.StartsWith(' '))
                line = line[1..];

            string trimmed = line.Trim();

            if (trimmed == "<summary>")
            {
                inSummary = true;
                continue;
            }

            if (trimmed == "</summary>")
            {
                inSummary = false;
                continue;
            }

            if (inSummary)
                body.Add(line);
        }

        if (body.Count == 0)
            return null;

        string joined = string.Join("\n", body);

        // Drop blank lines the analyser may leave between <summary> and the text.
        joined = joined.Trim('\n', '\r');
        while (joined.StartsWith('\n'))
            joined = joined[1..];

        if (joined.Length >= 2 && joined[0] == '"' && joined[^1] == '"')
            joined = joined[1..^1];

        return WebUtility.HtmlDecode(joined);
    }
}
