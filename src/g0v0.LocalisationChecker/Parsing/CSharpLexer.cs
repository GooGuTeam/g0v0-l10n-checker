// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;

namespace g0v0.LocalisationChecker.Parsing;

/// <summary>
/// A deliberately small single-pass C# lexer. It blanks out comments, string literals and character
/// literals so that structural regular expressions can never trip over a quote, comma, parenthesis
/// or <c>=&gt;</c> that happens to live inside a string.
/// </summary>
/// <remarks>
/// The masked output has exactly the same length as the input, and every masked position is
/// replaced by <see cref="Fill"/>. That keeps offsets (and therefore line numbers) directly
/// comparable with the original text. Callers locate a literal by skipping whitespace and looking
/// the resulting offset up in <see cref="ScanResult.ByStart"/>.
/// </remarks>
internal static class CSharpLexer
{
    internal const char Fill = '\u001F';

    internal sealed class ScanResult
    {
        public required string Masked { get; init; }

        public required IReadOnlyList<StringLiteral> Literals { get; init; }

        public required IReadOnlyDictionary<int, StringLiteral> ByStart { get; init; }

        /// <summary>
        /// Returns the literal that begins at or after <paramref name="from"/>, skipping whitespace,
        /// or null when the next token is not a string literal.
        /// </summary>
        public StringLiteral? LiteralAt(string masked, int from)
        {
            int p = from;
            while (p < masked.Length && char.IsWhiteSpace(masked[p]))
                p++;

            return ByStart.TryGetValue(p, out StringLiteral literal) ? literal : null;
        }

        /// <summary>Returns the first literal starting in <c>[from, limit)</c>, or null.</summary>
        public StringLiteral? NextLiteral(int from, int limit)
        {
            // Literals are collected in source order, so a linear scan is already ordered.
            foreach (StringLiteral literal in Literals)
            {
                if (literal.Start >= limit)
                    return null;

                if (literal.Start >= from)
                    return literal;
            }

            return null;
        }
    }

    public static ScanResult Scan(string source)
    {
        var masked = new StringBuilder(source);
        var literals = new List<StringLiteral>();

        int i = 0;
        while (i < source.Length)
        {
            char c = source[i];

            if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                int start = i;
                while (i < source.Length && source[i] != '\n')
                    i++;

                Blank(masked, start, i);
                continue;
            }

            if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
            {
                int start = i;
                i += 2;
                while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/'))
                    i++;

                i = Math.Min(i + 2, source.Length);
                Blank(masked, start, i);
                continue;
            }

            // Verbatim string: @"..." where "" denotes a single quote and real newlines are allowed.
            if (c == '"' && i + 2 < source.Length && source[i + 1] == '"' && source[i + 2] == '"')
            {
                int start = i;
                int quoteCount = 0;
                while (i + quoteCount < source.Length && source[i + quoteCount] == '"')
                    quoteCount++;

                i += quoteCount;
                var raw = new StringBuilder();

                while (i < source.Length)
                {
                    if (source[i] == '"')
                    {
                        int runLength = 0;
                        while (i + runLength < source.Length && source[i + runLength] == '"')
                            runLength++;

                        if (runLength >= quoteCount)
                        {
                            // Any surplus quotes belong to the content, e.g. """" contains one ".
                            raw.Append('"', runLength - quoteCount);
                            i += runLength;
                            break;
                        }

                        raw.Append('"', runLength);
                        i += runLength;
                        continue;
                    }

                    raw.Append(source[i++]);
                }

                literals.Add(new StringLiteral(start, i, CookRawString(raw.ToString()), true));
                Blank(masked, start, i);
                continue;
            }

            if (c == '@' && i + 1 < source.Length && source[i + 1] == '"')
            {
                int start = i;
                i += 2;
                var value = new StringBuilder();

                while (i < source.Length)
                {
                    if (source[i] == '"')
                    {
                        if (i + 1 < source.Length && source[i + 1] == '"')
                        {
                            value.Append('"');
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    value.Append(source[i++]);
                }

                literals.Add(new StringLiteral(start, i, value.ToString(), true));
                Blank(masked, start, i);
                continue;
            }

            if (c == '"')
            {
                int start = i;
                i++;
                var value = new StringBuilder();

                while (i < source.Length)
                {
                    char d = source[i];

                    if (d == '\\' && i + 1 < source.Length)
                    {
                        value.Append(Unescape(source[i + 1]));
                        i += 2;
                        continue;
                    }

                    if (d == '"')
                    {
                        i++;
                        break;
                    }

                    // Unterminated literal (not expected in generated files) - stop at the line end.
                    if (d == '\n')
                        break;

                    value.Append(d);
                    i++;
                }

                literals.Add(new StringLiteral(start, i, value.ToString(), false));
                Blank(masked, start, i);
                continue;
            }

            if (c == '\'')
            {
                int start = i;
                i++;
                while (i < source.Length)
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                    {
                        i += 2;
                        continue;
                    }

                    if (source[i] == '\'')
                    {
                        i++;
                        break;
                    }

                    if (source[i] == '\n')
                        break;

                    i++;
                }

                Blank(masked, start, i);
                continue;
            }

            i++;
        }

        var byStart = new Dictionary<int, StringLiteral>();
        foreach (StringLiteral literal in literals)
            byStart[literal.Start] = literal;

        return new ScanResult
        {
            Masked = masked.ToString(),
            Literals = literals,
            ByStart = byStart,
        };
    }

    private static char Unescape(char escaped) => escaped switch
    {
        'n' => '\n',
        'r' => '\r',
        't' => '\t',
        '0' => '\0',
        'a' => '\a',
        'b' => '\b',
        'f' => '\f',
        'v' => '\v',
        _ => escaped,
    };

    /// <summary>
    /// Applies C# 11 raw string literal semantics: the indentation of the closing delimiter is
    /// stripped from every line, and the newline that precedes the closing delimiter is dropped.
    /// Without this, comparing a raw literal against the member's XML doc would always disagree by
    /// exactly the indentation.
    /// </summary>
    private static string CookRawString(string raw)
    {
        string[] lines = raw.Split('\n');
        for (int i = 0; i < lines.Length; i++)
            lines[i] = lines[i].TrimEnd('\r');

        if (lines.Length == 1)
            return lines[0];

        // The final line holds only the whitespace in front of the closing delimiter.
        string indent = lines[^1];
        if (!indent.All(char.IsWhiteSpace))
            indent = string.Empty;

        var body = new List<string>(lines.Length);
        for (int i = 0; i < lines.Length - 1; i++)
            body.Add(lines[i]);

        if (body.Count > 0 && body[0].Length == 0)
            body.RemoveAt(0);

        if (indent.Length > 0)
        {
            for (int i = 0; i < body.Count; i++)
            {
                if (body[i].StartsWith(indent, StringComparison.Ordinal))
                    body[i] = body[i][indent.Length..];
            }
        }

        return string.Join("\n", body);
    }

    private static void Blank(StringBuilder builder, int start, int end)
    {
        for (int k = start; k < end && k < builder.Length; k++)
            builder[k] = Fill;
    }
}
