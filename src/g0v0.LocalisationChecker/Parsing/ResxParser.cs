// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Xml;
using System.Xml.Linq;
using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Parsing;

/// <summary>
/// Parses a base resx file. An XML parser is used rather than a regex because the repository
/// contains two different <c>&lt;data&gt;</c> layouts (single-line and multi-line indented) and
/// values that legitimately contain real newlines.
/// </summary>
internal static class ResxParser
{
    public static IReadOnlyList<ResourceEntry> Parse(string path)
    {
        XDocument document = XDocument.Load(
            path,
            LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

        XElement? root = document.Root;
        if (root is null)
            return [];

        var entries = new List<ResourceEntry>();

        foreach (XElement data in root.Elements("data"))
        {
            string? name = data.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name))
                continue;

            string value = data.Element("value")?.Value ?? string.Empty;
            int line = data is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : 0;

            entries.Add(new ResourceEntry(name, value, line));
        }

        return entries;
    }

    /// <summary>
    /// True when the file name carries no culture suffix, i.e. it is the English source rather than
    /// a Crowdin-generated translation. <c>Common.resx</c> is a base file; <c>Common.zh.resx</c>,
    /// <c>Common.zh-Hant.resx</c> and <c>Common.es-419.resx</c> are not.
    /// </summary>
    public static bool IsBaseResource(string path)
    {
        string name = Path.GetFileName(path);
        return name.EndsWith(".resx", StringComparison.OrdinalIgnoreCase)
               && name.Count(c => c == '.') == 1;
    }
}
