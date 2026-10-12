// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.Json.Nodes;
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

        string[] lines = Render(new GitHubReporter(), messy);

        Assert.That(lines[0], Is.EqualTo(
            "::error file=osu.Game/Localisation/HUD/SpectatorListStrings.cs,line=10,title=Prefix mismatch"
            + "::line one%0Aline two 100%25 a:b,c"));
    }

    [Test]
    public void Github_reporter_maps_severity_onto_the_right_command()
    {
        Assert.That(Render(new GitHubReporter(), sample with { Severity = Severity.Warning })[0], Does.StartWith("::warning "));
        Assert.That(Render(new GitHubReporter(), sample with { Severity = Severity.Info })[0], Does.StartWith("::notice "));
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
}
