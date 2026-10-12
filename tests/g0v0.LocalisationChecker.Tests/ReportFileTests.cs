// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Tests;

/// <summary>
/// Every file this tool writes is either committed (the baseline) or read back by another tool (the
/// JSON and SARIF reports), so its bytes must not depend on the machine that produced them. The
/// failure is invisible on Linux and obvious in a diff on Windows, which is the wrong way round — so
/// these tests drive the real CLI and look for the carriage return that
/// <see cref="Environment.NewLine"/> would have emitted there.
/// </summary>
[TestFixture]
public class ReportFileTests
{
    private static string FixtureDirectory(string name)
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

        while (directory is not null &&
               !Directory.Exists(System.IO.Path.Combine(directory.FullName, "tests", "fixtures", name)))
        {
            directory = directory.Parent;
        }

        Assert.That(directory, Is.Not.Null, $"could not find tests/fixtures/{name} above the test directory");

        return System.IO.Path.Combine(directory!.FullName, "tests", "fixtures", name);
    }

    [Test]
    public void The_written_files_use_lf_on_every_platform()
    {
        using Fixture.TempDirectory temp = Fixture.NewTempDirectory();

        string baseline = System.IO.Path.Combine(temp.Path, "baseline.json");
        string json = System.IO.Path.Combine(temp.Path, "localisation.json");
        string sarif = System.IO.Path.Combine(temp.Path, "localisation.sarif");

        int exitCode = Program.Main([
            "--osu", FixtureDirectory("osu"),
            "--resources", FixtureDirectory("resources"),
            "--format", "json",
            "--color", "never",
            "--fail-on", "never",
            "--no-baseline",
            "--json-report", json,
            "--sarif-report", sarif,
            "--sarif-source", "osu",
            "--write-baseline", baseline,
        ]);

        Assert.That(exitCode, Is.EqualTo(0), "the CLI should report rather than fail on the fixtures");

        AssertLfOnly(baseline);
        AssertLfOnly(json);
        AssertLfOnly(sarif);
    }

    private static void AssertLfOnly(string path)
    {
        string name = System.IO.Path.GetFileName(path);

        Assert.That(File.Exists(path), Is.True, $"{name} was not written");

        string text = File.ReadAllText(path);

        Assert.That(text, Is.Not.Empty, $"{name} is empty");
        Assert.That(text, Does.Contain('\n'), $"{name} should be line separated");
        Assert.That(text, Does.Not.Contain('\r'), $"{name} should be written with LF only");
    }
}
