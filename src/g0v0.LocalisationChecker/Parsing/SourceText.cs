// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Parsing;

/// <summary>
/// A line index over a source file. Offsets are always measured against the original text, never
/// against the masked text produced by <see cref="CSharpLexer"/> (which preserves offsets anyway).
/// </summary>
internal sealed class SourceText
{
    private readonly int[] lineStarts;

    public SourceText(string text)
    {
        Text = text;

        string[] raw = text.Split('\n');
        for (int i = 0; i < raw.Length; i++)
            raw[i] = raw[i].TrimEnd('\r');

        Lines = raw;

        var starts = new List<int>(raw.Length) { 0 };
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                starts.Add(i + 1);
        }

        lineStarts = [.. starts];
    }

    public string Text { get; }

    /// <summary>Lines with trailing CR removed.</summary>
    public string[] Lines { get; }

    /// <summary>1-based line number containing <paramref name="offset"/>.</summary>
    public int LineOf(int offset)
    {
        if (offset <= 0)
            return 1;

        int lo = 0;
        int hi = lineStarts.Length - 1;

        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (lineStarts[mid] <= offset)
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo + 1;
    }
}
