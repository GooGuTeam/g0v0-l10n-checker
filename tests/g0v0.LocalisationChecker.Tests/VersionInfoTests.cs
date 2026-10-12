// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using g0v0.LocalisationChecker.Cli;

namespace g0v0.LocalisationChecker.Tests;

/// <summary>
/// The version is only useful if it is the version of the binary that reports it, so these tests
/// check the value against the assembly attributes the SDK generated rather than against a literal.
/// </summary>
[TestFixture]
public class VersionInfoTests
{
    private static string informationalVersion =>
        typeof(VersionInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    [Test]
    public void Version_is_read_from_the_informational_version_attribute()
    {
        string informational = informationalVersion;
        int separator = informational.IndexOf('+');

        string expected = separator < 0 ? informational : informational[..separator];

        Assert.That(VersionInfo.Version, Is.EqualTo(expected));
    }

    [Test]
    public void Version_is_a_semantic_version_without_build_metadata()
    {
        Assert.That(VersionInfo.Version, Does.Match(@"^\d+\.\d+\.\d+"));
        Assert.That(VersionInfo.Version, Does.Not.Contain("+"));
    }

    [Test]
    public void Product_matches_the_assembly_product_attribute()
    {
        AssemblyProductAttribute? attribute = typeof(VersionInfo).Assembly.GetCustomAttribute<AssemblyProductAttribute>();

        Assert.That(attribute, Is.Not.Null, "Directory.Build.props should declare <Product>");
        Assert.That(VersionInfo.Product, Is.EqualTo(attribute!.Product));
        Assert.That(VersionInfo.Product, Is.Not.Empty);
    }

    [Test]
    public void Revision_is_reported_only_when_the_build_recorded_one()
    {
        string informational = informationalVersion;
        int separator = informational.IndexOf('+');
        string? expected = separator >= 0 && separator < informational.Length - 1
            ? informational[(separator + 1)..]
            : null;

        Assert.That(VersionInfo.Revision, Is.EqualTo(expected));
    }

    [Test]
    public void Display_is_the_product_followed_by_the_version()
    {
        string expected = VersionInfo.Revision is null
            ? $"{VersionInfo.Product} {VersionInfo.Version}"
            : $"{VersionInfo.Product} {VersionInfo.Version}+{VersionInfo.Revision}";

        Assert.That(VersionInfo.Display, Is.EqualTo(expected));
    }

    [Test]
    public void Version_flag_prints_the_display_banner()
    {
        TextWriter original = Console.Out;

        try
        {
            var buffer = new StringWriter();
            Console.SetOut(buffer);

            int exitCode = Program.Main(["--version"]);

            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(buffer.ToString().Trim(), Is.EqualTo(VersionInfo.Display));
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    [Test]
    public void Help_text_carries_the_product_name_from_the_same_source()
    {
        Assert.That(CliHelp.Text, Does.Contain(VersionInfo.Product));
        Assert.That(CliHelp.Text, Does.Not.Contain("{{"), "the raw string interpolation should have been resolved");
    }
}
