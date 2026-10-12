// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Cli;
using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Output;

namespace g0v0.LocalisationChecker.Tests;

[TestFixture]
public class CliParserTests
{
    [Test]
    public void Defaults_are_the_standard_local_checkout_layout()
    {
        CliOptions options = CliParser.Parse([]);

        Assert.That(options.OsuRoot, Is.EqualTo(CliOptions.DefaultOsuRoot));
        Assert.That(options.ResourcesRoot, Is.EqualTo(CliOptions.DefaultResourcesRoot));
        Assert.That(options.Color, Is.EqualTo(ColorMode.Auto));
        Assert.That(options.Format, Is.EqualTo(OutputFormat.Auto));
        Assert.That(options.MinSeverity, Is.EqualTo(Severity.Info));
        Assert.That(options.FailOn, Is.EqualTo(FailOn.Error));
    }

    [Test]
    public void Accepts_both_separated_and_inline_values()
    {
        CliOptions separated = CliParser.Parse(["--osu", "/a", "--resources", "/b"]);
        CliOptions inline = CliParser.Parse(["--osu=/a", "--resources=/b"]);

        Assert.That(separated.OsuRoot, Is.EqualTo("/a"));
        Assert.That(inline.OsuRoot, Is.EqualTo("/a"));
        Assert.That(inline.ResourcesRoot, Is.EqualTo("/b"));
    }

    [Test]
    public void Accepts_short_names()
    {
        CliOptions options = CliParser.Parse(["-o", "/a", "-r", "/b", "-c", "never", "-f", "json"]);

        Assert.That(options.OsuRoot, Is.EqualTo("/a"));
        Assert.That(options.Color, Is.EqualTo(ColorMode.Never));
        Assert.That(options.Format, Is.EqualTo(OutputFormat.Json));
    }

    [Test]
    public void Parses_extra_resx_with_and_without_a_namespace()
    {
        CliOptions options = CliParser.Parse(
        [
            "-x", "Custom/Localisation=Custom.Localisation",
            "-x", "Other/Resx",
        ]);

        Assert.That(options.ExtraResx, Has.Count.EqualTo(2));
        Assert.That(options.ExtraResx[0].Path, Is.EqualTo("Custom/Localisation"));
        Assert.That(options.ExtraResx[0].RootNamespace, Is.EqualTo("Custom.Localisation"));
        Assert.That(options.ExtraResx[1].RootNamespace, Is.Empty);
    }

    [Test]
    public void Canonicalises_rule_ids_so_lookups_always_hit()
    {
        CliOptions options = CliParser.Parse(["--ignore-rule", "l10n008", "--severity", "l10n005=warning"]);

        Assert.That(options.IgnoreRules, Is.EqualTo(new[] { "L10N008" }));
        Assert.That(options.SeverityOverrides["L10N005"], Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void Quiet_implies_error_only_and_no_summary()
    {
        CliOptions options = CliParser.Parse(["--quiet"]);

        Assert.That(options.MinSeverity, Is.EqualTo(Severity.Error));
        Assert.That(options.NoSummary, Is.True);
    }

    [Test]
    public void Keeps_the_json_report_separate_from_the_display_format()
    {
        CliOptions options = CliParser.Parse(["--format", "github", "--json-report", "/tmp/report.json"]);

        Assert.That(options.Format, Is.EqualTo(OutputFormat.GitHub));
        Assert.That(options.JsonReportPath, Is.EqualTo("/tmp/report.json"));
    }

    [Test]
    public void Defaults_the_sarif_report_to_the_osu_side()
    {
        CliOptions options = CliParser.Parse(["--sarif-report", "/tmp/report.sarif"]);

        Assert.That(options.SarifReportPath, Is.EqualTo("/tmp/report.sarif"));
        Assert.That(options.SarifSource, Is.EqualTo(DiagnosticSource.Osu));
    }

    [Test]
    public void Accepts_the_resources_side_for_the_sarif_report()
    {
        CliOptions options = CliParser.Parse(["--sarif-source", "RESOURCES"]);

        Assert.That(options.SarifSource, Is.EqualTo(DiagnosticSource.Resources));
    }

    [TestCase("--bogus")]
    [TestCase("--color", "sometimes")]
    [TestCase("--format", "xml")]
    [TestCase("--min-severity", "loud")]
    [TestCase("--fail-on", "maybe")]
    [TestCase("--ignore-rule", "L10N999")]
    [TestCase("--severity", "L10N008")]
    [TestCase("--no-summary=yes")]
    [TestCase("--osu")]
    [TestCase("--sarif-source", "extra")]
    public void Rejects_bad_input(params string[] args)
        => Assert.Throws<CliException>(() => CliParser.Parse(args));

    [Test]
    public void Rejects_a_positional_argument()
        => Assert.Throws<CliException>(() => CliParser.Parse(["osu"]));
}
