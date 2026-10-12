// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Parsing;

namespace g0v0.LocalisationChecker.Tests;

[TestFixture]
public class ResxParserTests
{
    /// <summary>
    /// Deliberately mixes the two layouts that exist in osu-resources (indented multi-line
    /// <c>&lt;data&gt;</c> and single-line ones) and keeps a real newline inside a value.
    /// </summary>
    private static readonly string content = string.Join("\n",
    [
        "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
        "<root>",
        "  <resheader name=\"resmimetype\">",
        "    <value>text/microsoft-resx</value>",
        "  </resheader>",
        "  <resheader name=\"version\">",
        "    <value>2.0</value>",
        "  </resheader>",
        "  <data name=\"back\" xml:space=\"preserve\">",
        "    <value>Back</value>",
        "  </data>",
        "  <data name=\"next\" xml:space=\"preserve\"><value>Next</value></data>",
        "  <data name=\"multi\" xml:space=\"preserve\">",
        "    <value>Line one",
        "Line two</value>",
        "  </data>",
        "</root>",
    ]);

    [Test]
    public void Parses_both_data_layouts()
    {
        using Fixture.TempDirectory directory = Fixture.NewTempDirectory();
        string path = directory.Write("Test.resx", content);

        IReadOnlyList<ResourceEntry> entries = ResxParser.Parse(path);

        Assert.That(entries.Select(e => e.Name), Is.EqualTo(new[] { "back", "next", "multi" }));
        Assert.That(entries[0].Value, Is.EqualTo("Back"));
        Assert.That(entries[1].Value, Is.EqualTo("Next"));
    }

    [Test]
    public void Preserves_real_newlines_inside_values()
    {
        using Fixture.TempDirectory directory = Fixture.NewTempDirectory();
        string path = directory.Write("Test.resx", content);

        ResourceEntry multi = ResxParser.Parse(path).Single(e => e.Name == "multi");

        Assert.That(multi.Value, Is.EqualTo("Line one\nLine two"));
    }

    [Test]
    public void Reports_the_line_of_each_entry()
    {
        using Fixture.TempDirectory directory = Fixture.NewTempDirectory();
        string path = directory.Write("Test.resx", content);

        IReadOnlyList<ResourceEntry> entries = ResxParser.Parse(path);

        Assert.That(entries.Single(e => e.Name == "back").Line, Is.EqualTo(9));
    }

    [Test]
    public void Reads_files_with_a_byte_order_mark()
    {
        using Fixture.TempDirectory directory = Fixture.NewTempDirectory();
        string path = Path.Combine(directory.Path, "Bom.resx");

        File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        Assert.That(ResxParser.Parse(path), Has.Count.EqualTo(3));
    }

    [TestCase("Common.resx", true)]
    [TestCase("Common.zh.resx", false)]
    [TestCase("Common.zh-Hant.resx", false)]
    [TestCase("Common.es-419.resx", false)]
    [TestCase("Common.zh-Hans.resx", false)]
    public void Identifies_base_resources(string fileName, bool expected)
        => Assert.That(ResxParser.IsBaseResource(fileName), Is.EqualTo(expected));
}
