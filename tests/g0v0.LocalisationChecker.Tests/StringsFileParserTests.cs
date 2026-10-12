// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Parsing;

namespace g0v0.LocalisationChecker.Tests;

[TestFixture]
public class StringsFileParserTests
{
    private static readonly string simpleSource = string.Join("\n",
    [
        "namespace X",
        "{",
        "    public static class SimpleStrings",
        "    {",
        "        private const string prefix = @\"osu.Game.Resources.Localisation.Simple\";",
        "",
        "        /// <summary>",
        "        /// \"Back\"",
        "        /// </summary>",
        "        public static LocalisableString Back => new TranslatableString(getKey(@\"back\"), @\"Back\");",
        "",
        "        private static string getKey(string key) => $@\"{prefix}:{key}\";",
        "    }",
        "}",
    ]);

    [Test]
    public void Reads_prefix_class_name_and_member()
    {
        LocalisationClass? parsed = Parse(simpleSource);

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Prefix, Is.EqualTo("osu.Game.Resources.Localisation.Simple"));
        Assert.That(parsed.ClassName, Is.EqualTo("SimpleStrings"));
        Assert.That(parsed.PrefixLine, Is.EqualTo(5));
        Assert.That(parsed.Members, Has.Count.EqualTo(1));

        LocalisationMember member = parsed.Members[0];
        Assert.That(member.Name, Is.EqualTo("Back"));
        Assert.That(member.Key, Is.EqualTo("back"));
        Assert.That(member.Fallback, Is.EqualTo("Back"));
        Assert.That(member.Documentation, Is.EqualTo("Back"));
        Assert.That(member.Kind, Is.EqualTo(MemberKind.Property));
        Assert.That(member.Line, Is.EqualTo(10));
    }

    [Test]
    public void Returns_null_when_there_is_no_prefix()
    {
        string source = string.Join("\n",
        [
            "public static class NotLocalised",
            "{",
            "    public static string Something => \"nope\";",
            "}",
        ]);

        Assert.That(Parse(source), Is.Null);
    }

    private static readonly string complexSource = string.Join("\n",
    [
        "namespace X",
        "{",
        "    public static class ComplexStrings",
        "    {",
        "        private const string prefix = @\"osu.Game.Resources.Localisation.Complex\";",
        "",
        "        /// <summary>",
        "        /// \"Say &quot;hi&quot;\"",
        "        /// </summary>",
        "        public static LocalisableString Quoted => new TranslatableString(getKey(@\"quoted\"), @\"Say \"\"hi\"\"\");",
        "",
        "        /// <summary>",
        "        /// \"First line",
        "        ///",
        "        /// second line\"",
        "        /// </summary>",
        "        public static LocalisableString Multiline => new TranslatableString(getKey(@\"multiline\"), @\"First line",
        "",
        "second line\");",
        "",
        "        /// <summary>",
        "        /// \"Raw line one",
        "        /// raw line two\"",
        "        /// </summary>",
        "        public static LocalisableString Raw => new TranslatableString(getKey(@\"raw\"), \"\"\"",
        "            Raw line one",
        "            raw line two",
        "            \"\"\");",
        "",
        "        /// <summary>",
        "        /// \"{0} item|{0} items\"",
        "        /// </summary>",
        "        public static LocalisableString Items(int quantity)",
        "            => new PluralisableString(new TranslatableString(getKey(@\"items\"), @\"{0} item|{0} items\", quantity), quantity, '|');",
        "",
        "        private static string getKey(string key) => $@\"{prefix}:{key}\";",
        "    }",
        "}",
    ]);

    [Test]
    public void Handles_escaped_quotes_in_verbatim_literals()
    {
        LocalisationMember member = MemberOf("Quoted");

        Assert.That(member.Key, Is.EqualTo("quoted"));
        Assert.That(member.Fallback, Is.EqualTo("Say \"hi\""));
        Assert.That(member.Documentation, Is.EqualTo("Say \"hi\""));
    }

    [Test]
    public void Handles_multiline_verbatim_literals()
    {
        LocalisationMember member = MemberOf("Multiline");

        Assert.That(member.Fallback, Is.EqualTo("First line\n\nsecond line"));
        Assert.That(member.Documentation, Is.EqualTo("First line\n\nsecond line"));
    }

    [Test]
    public void Handles_raw_string_literals()
    {
        LocalisationMember member = MemberOf("Raw");

        Assert.That(member.Key, Is.EqualTo("raw"));
        Assert.That(member.Fallback, Is.EqualTo("Raw line one\nraw line two"));
        Assert.That(member.Documentation, Is.EqualTo("Raw line one\nraw line two"));
    }

    [Test]
    public void Handles_pluralisable_strings()
    {
        LocalisationMember member = MemberOf("Items");

        Assert.That(member.Kind, Is.EqualTo(MemberKind.Plural));
        Assert.That(member.Key, Is.EqualTo("items"));
        Assert.That(member.Fallback, Is.EqualTo("{0} item|{0} items"));
    }

    [Test]
    public void Handles_arrow_on_the_next_line()
    {
        Assert.That(MemberOf("Items").Line, Is.EqualTo(33));
    }

    private static LocalisationMember MemberOf(string name)
    {
        LocalisationClass? parsed = Parse(complexSource);

        Assert.That(parsed, Is.Not.Null);

        LocalisationMember? member = parsed!.Members.SingleOrDefault(m => m.Name == name);
        Assert.That(member, Is.Not.Null, $"member '{name}' was not parsed");

        return member!;
    }

    private static LocalisationClass? Parse(string source)
        => StringsFileParser.Parse("TestStrings.cs", "TestStrings.cs", string.Empty, source);
}
