// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.Json.Nodes;
using g0v0.LocalisationChecker.Checking;
using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Output;

namespace g0v0.LocalisationChecker.Tests;

[TestFixture]
public class OutputTests
{
    private static readonly Diagnostic sample = new(
        "L10N003",
        Severity.Error,
        DiagnosticSource.Osu,
        "osu.Game/Localisation/HUD/SpectatorListStrings.cs",
        10,
        "prefix 'a' does not match; expected 'b'",
        Key: "prefix-key");

    private static ReportSummary Summary(int errors = 1, int warnings = 0, int infos = 0, int suppressed = 0) => new()
    {
        Classes = 1,
        Resources = 2,
        SkippedStringsFiles = 0,
        Errors = errors,
        Warnings = warnings,
        Infos = infos,
        Suppressed = suppressed,
    };

    private static string[] Render(IReporter reporter, params Diagnostic[] diagnostics)
    {
        var writer = new StringWriter();
        reporter.Report(diagnostics, Summary(diagnostics.Length), writer);

        return writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [Test]
    public void Human_reporter_uses_the_file_line_message_shape()
    {
        string[] lines = Render(new HumanReporter(Ansi.Plain, showSummary: false), sample);

        Assert.That(lines, Is.EqualTo(new[] { "osu.Game/Localisation/HUD/SpectatorListStrings.cs:10: error L10N003: prefix 'a' does not match; expected 'b'" }));
    }

    [Test]
    public void Human_reporter_omits_the_line_when_there_is_none()
    {
        Diagnostic fileLevel = sample with { Line = 0 };

        string[] lines = Render(new HumanReporter(Ansi.Plain, showSummary: false), fileLevel);

        Assert.That(lines[0], Does.StartWith("osu.Game/Localisation/HUD/SpectatorListStrings.cs: error L10N003:"));
    }

    [Test]
    public void Human_reporter_adds_escape_sequences_only_when_colouring()
    {
        string plain = Render(new HumanReporter(Ansi.Plain, showSummary: false), sample)[0];
        string coloured = Render(new HumanReporter(Ansi.Coloured, showSummary: false), sample)[0];

        Assert.That(plain, Does.Not.Contain("\u001b["));
        Assert.That(coloured, Does.Contain("\u001b["));
        Assert.That(coloured, Does.Contain(new string(sample.Message.Take(10).ToArray())));
    }

    [Test]
    public void Github_reporter_escapes_workflow_command_payloads()
    {
        Diagnostic messy = sample with { Message = "line one\nline two 100% a:b,c" };

        string[] lines = Render(new GitHubReporter(showSummary: true), messy);

        Assert.That(lines[0], Is.EqualTo(
            "::error file=osu.Game/Localisation/HUD/SpectatorListStrings.cs,line=10,title=Prefix mismatch"
            + "::line one%0Aline two 100%25 a:b,c"));
    }

    [Test]
    public void Github_reporter_maps_severity_onto_the_right_command()
    {
        Assert.That(Render(new GitHubReporter(showSummary: true), sample with { Severity = Severity.Warning })[0], Does.StartWith("::warning "));
        Assert.That(Render(new GitHubReporter(showSummary: true), sample with { Severity = Severity.Info })[0], Does.StartWith("::notice "));
    }

    [Test]
    public void Github_reporter_emits_a_summary_notice_by_default()
    {
        string[] lines = Render(new GitHubReporter(showSummary: true), sample);

        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(lines[1], Does.StartWith("::notice title=localisation-checker::Checked 1 classes / 2 resources"));
    }

    [Test]
    public void Github_reporter_omits_the_summary_when_asked()
    {
        string[] lines = Render(new GitHubReporter(showSummary: false), sample);

        Assert.That(lines, Has.Length.EqualTo(1));
    }

    [Test]
    public void Json_reporter_emits_a_versioned_document()
    {
        var writer = new StringWriter();
        new JsonReporter("/osu", "/resources").Report([sample], Summary(), writer);

        JsonNode document = JsonNode.Parse(writer.ToString())!;

        Assert.That((int)document["version"]!, Is.EqualTo(1));
        Assert.That((string)document["osuRoot"]!, Is.EqualTo("/osu"));

        JsonNode tool = document["tool"]!;
        Assert.That((string)tool["name"]!, Is.EqualTo(VersionInfo.Product));
        Assert.That((string)tool["version"]!, Is.EqualTo(VersionInfo.Version));

        JsonNode entry = document["diagnostics"]![0]!;
        Assert.That((string)entry["rule"]!, Is.EqualTo("L10N003"));
        Assert.That((string)entry["severity"]!, Is.EqualTo("error"));
        Assert.That((string)entry["source"]!, Is.EqualTo("osu"));
        Assert.That((int)entry["line"]!, Is.EqualTo(10));
        Assert.That((string)entry["key"]!, Is.EqualTo("prefix-key"));
        Assert.That((int)document["summary"]!["error"]!, Is.EqualTo(1));
    }

    private static JsonNode Sarif(DiagnosticSource source, params Diagnostic[] diagnostics)
    {
        var writer = new StringWriter();
        new SarifReporter(source).Report(diagnostics, Summary(diagnostics.Length), writer);

        return JsonNode.Parse(writer.ToString())!;
    }

    private static JsonNode OnlyResult(JsonNode sarif) => sarif["runs"]![0]!["results"]![0]!;

    [Test]
    public void Sarif_reporter_emits_the_shape_code_scanning_requires()
    {
        JsonNode sarif = Sarif(DiagnosticSource.Osu, sample);

        Assert.That((string)sarif["version"]!, Is.EqualTo("2.1.0"));
        Assert.That((string)sarif["$schema"]!, Is.EqualTo("https://json.schemastore.org/sarif-2.1.0.json"));

        JsonNode driver = sarif["runs"]![0]!["tool"]!["driver"]!;
        Assert.That((string)driver["name"]!, Is.EqualTo(VersionInfo.Product));
        Assert.That((string)driver["version"]!, Is.EqualTo(VersionInfo.Version));

        // Every rule needs these four, and code scanning refuses the upload without them.
        JsonArray rules = driver["rules"]!.AsArray();
        Assert.That(rules.Select(rule => (string)rule!["id"]!), Is.EqualTo(RuleIds.All));
        Assert.That(rules.Select(rule => (string)rule!["shortDescription"]!["text"]!), Is.All.Not.Empty);
        Assert.That(rules.Select(rule => (string)rule!["fullDescription"]!["text"]!), Is.All.Not.Empty);
        Assert.That(rules.Select(rule => (string)rule!["help"]!["text"]!), Is.All.Not.Empty);
        Assert.That(rules.Select(rule => (string)rule!["helpUri"]!), Is.All.EqualTo(RuleIds.DocumentationUrl));

        JsonNode result = sarif["runs"]![0]!["results"]![0]!;
        Assert.That((string)result["ruleId"]!, Is.EqualTo("L10N003"));
        Assert.That((string)result["level"]!, Is.EqualTo("error"));
        Assert.That((string)result["message"]!["text"]!, Is.EqualTo(sample.Message));

        // ruleIndex has to point at the descriptor with the same id, or GitHub marks the wrong rule.
        var index = (int)result["ruleIndex"]!;
        Assert.That((string)rules[index]!["id"]!, Is.EqualTo((string)result["ruleId"]!));

        JsonNode physical = result["locations"]![0]!["physicalLocation"]!;
        Assert.That((string)physical["artifactLocation"]!["uri"]!, Is.EqualTo(sample.File));

        JsonNode region = physical["region"]!;
        Assert.That((int)region["startLine"]!, Is.EqualTo(10));
        Assert.That((int)region["endLine"]!, Is.EqualTo(10));
        Assert.That((int)region["startColumn"]!, Is.GreaterThanOrEqualTo(1));
        Assert.That((int)region["endColumn"]!, Is.GreaterThanOrEqualTo((int)region["startColumn"]!));

        Assert.That((string)result["partialFingerprints"]!["primaryLocationLineHash"]!, Is.Not.Empty);
    }

    [Test]
    public void Sarif_reporter_maps_every_severity_onto_a_level()
    {
        Assert.That((string)OnlyResult(Sarif(DiagnosticSource.Osu, sample with { Severity = Severity.Warning }))["level"]!, Is.EqualTo("warning"));
        Assert.That((string)OnlyResult(Sarif(DiagnosticSource.Osu, sample with { Severity = Severity.Info }))["level"]!, Is.EqualTo("note"));
    }

    [Test]
    public void Sarif_reporter_starts_a_file_level_finding_at_line_one()
    {
        JsonNode region = OnlyResult(Sarif(DiagnosticSource.Osu, sample with { Line = 0 }))
            ["locations"]![0]!["physicalLocation"]!["region"]!;

        Assert.That((int)region["startLine"]!, Is.EqualTo(1));
        Assert.That((int)region["endLine"]!, Is.EqualTo(1));
    }

    [Test]
    public void Sarif_reporter_only_describes_the_repository_it_was_asked_for()
    {
        Diagnostic resources = sample with
        {
            RuleId = RuleIds.OrphanResourceKey,
            Severity = Severity.Warning,
            Source = DiagnosticSource.Resources,
            File = "osu.Game.Resources/Localisation/HUD/SpectatorList.resx",
            Key = "spare",
        };

        JsonNode osu = Sarif(DiagnosticSource.Osu, sample, resources);
        JsonNode resourcesOnly = Sarif(DiagnosticSource.Resources, sample, resources);

        Assert.That(osu["runs"]![0]!["results"]!.AsArray(), Has.Count.EqualTo(1));
        Assert.That((string)OnlyResult(osu)["ruleId"]!, Is.EqualTo("L10N003"));

        Assert.That(resourcesOnly["runs"]![0]!["results"]!.AsArray(), Has.Count.EqualTo(1));
        Assert.That((string)OnlyResult(resourcesOnly)["ruleId"]!, Is.EqualTo("L10N002"));
    }

    /// <summary>
    /// The whole point of writing our own fingerprint: an alert keeps its identity when the code
    /// around it moves, so code scanning does not close one alert and open another.
    /// </summary>
    [Test]
    public void Sarif_fingerprint_survives_a_line_move_but_not_a_different_key()
    {
        static string Fingerprint(Diagnostic diagnostic)
            => (string)OnlyResult(Sarif(DiagnosticSource.Osu, diagnostic))["partialFingerprints"]!["primaryLocationLineHash"]!;

        string before = Fingerprint(sample);
        string after = Fingerprint(sample with { Line = sample.Line + 500 });

        Assert.That(after, Is.EqualTo(before));
        Assert.That(before, Does.EndWith(":1"));
        Assert.That(Fingerprint(sample with { Key = "other-key" }), Is.Not.EqualTo(before));
    }
}
