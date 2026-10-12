// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Checking;
using g0v0.LocalisationChecker.Checking.Checkers;
using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Workspace;

namespace g0v0.LocalisationChecker.Tests;

[TestFixture]
public class CheckerTests
{
    private const string ns = "osu.Game.Resources.Localisation";

    [Test]
    public void L10N001_reports_a_key_that_is_not_in_the_resource()
    {
        CheckContext context = Build(
            [Fixture.Class("FooStrings", $"{ns}.Foo", [Fixture.Member("Bar", "bar")])],
            [Fixture.Resource($"{ns}.Foo", ["other"])]);

        List<Diagnostic> diagnostics = Run<MissingResourceChecker>(context);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Key, Is.EqualTo("bar"));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(Severity.Error));
    }

    [Test]
    public void L10N001_is_silent_when_the_key_exists()
    {
        CheckContext context = Build(
            [Fixture.Class("FooStrings", $"{ns}.Foo", [Fixture.Member("Bar", "bar")])],
            [Fixture.Resource($"{ns}.Foo", ["bar"])]);

        Assert.That(Run<MissingResourceChecker>(context), Is.Empty);
    }

    [Test]
    public void L10N002_reports_a_resource_key_no_member_declares()
    {
        CheckContext context = Build(
            [Fixture.Class("FooStrings", $"{ns}.Foo", [Fixture.Member("Bar", "bar")])],
            [Fixture.Resource($"{ns}.Foo", ["bar", "gone"])]);

        List<Diagnostic> diagnostics = Run<OrphanResourceKeyChecker>(context);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Key, Is.EqualTo("gone"));
    }

    [Test]
    public void L10N002_ignores_resources_outside_the_reverse_scope()
    {
        CheckContext context = Build(
            [],
            [Fixture.Resource($"{ns}.Web.Foo", ["gone"], inReverseScope: false)]);

        Assert.That(Run<OrphanResourceKeyChecker>(context), Is.Empty);
    }

    [Test]
    public void L10N003_reports_a_prefix_that_matches_nothing()
    {
        CheckContext context = Build(
            [Fixture.Class("SpectatorListStrings", $"{ns}.SpectatorList", [Fixture.Member("SpectatorCount", "spectator_count")], "HUD")],
            [Fixture.Resource($"{ns}.HUD.SpectatorList", ["spectator_count"], "HUD")]);

        List<Diagnostic> diagnostics = Run<PrefixMismatchChecker>(context);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Suggestion, Is.EqualTo($"{ns}.HUD.SpectatorList"));
        Assert.That(diagnostics[0].Line, Is.EqualTo(1));
    }

    [Test]
    public void L10N003_is_silent_for_a_correct_prefix()
    {
        CheckContext context = Build(
            [Fixture.Class("FooStrings", $"{ns}.Foo", [Fixture.Member("Bar", "bar")])],
            [Fixture.Resource($"{ns}.Foo", ["bar"])]);

        Assert.That(Run<PrefixMismatchChecker>(context), Is.Empty);
    }

    [Test]
    public void L10N004_reports_a_key_used_twice_in_one_class()
    {
        CheckContext context = Build(
            [Fixture.Class("FooStrings", $"{ns}.Foo", [Fixture.Member("A", "dup"), Fixture.Member("B", "dup", line: 9)])],
            [Fixture.Resource($"{ns}.Foo", ["dup"])]);

        List<Diagnostic> diagnostics = Run<DuplicateKeyChecker>(context);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Line, Is.EqualTo(9));
        Assert.That(diagnostics[0].Message, Does.Contain("line 1"));
    }

    [Test]
    public void L10N004_is_silent_when_every_key_is_unique()
    {
        CheckContext context = Build(
            [Fixture.Class("FooStrings", $"{ns}.Foo", [Fixture.Member("A", "a"), Fixture.Member("B", "b")])],
            [Fixture.Resource($"{ns}.Foo", ["a", "b"])]);

        Assert.That(Run<DuplicateKeyChecker>(context), Is.Empty);
    }

    [Test]
    public void L10N005_reports_documentation_that_disagrees_with_the_fallback()
    {
        CheckContext context = Build(
            [Fixture.Class("FooStrings", $"{ns}.Foo", [Fixture.Member("A", "a", "Next", "Back")])],
            [Fixture.Resource($"{ns}.Foo", ["a"])]);

        Assert.That(Run<DocFallbackMismatchChecker>(context), Has.Count.EqualTo(1));
    }

    [Test]
    public void L10N005_ignores_line_ending_differences()
    {
        CheckContext context = Build(
            [Fixture.Class("FooStrings", $"{ns}.Foo", [Fixture.Member("A", "a", "line1\r\nline2", "line1\nline2")])],
            [Fixture.Resource($"{ns}.Foo", ["a"])]);

        Assert.That(Run<DocFallbackMismatchChecker>(context), Is.Empty);
    }

    [Test]
    public void L10N006_reports_a_base_resource_without_a_zh_pair()
    {
        using Fixture.TempDirectory directory = Fixture.NewTempDirectory();
        string basePath = directory.Write("Foo.resx", "<root />");

        CheckContext context = Build([], [Fixture.Resource($"{ns}.Foo", [], absolutePath: basePath)]);

        Assert.That(Run<MissingZhChecker>(context), Has.Count.EqualTo(1));
    }

    [Test]
    public void L10N006_is_silent_when_the_zh_pair_exists()
    {
        using Fixture.TempDirectory directory = Fixture.NewTempDirectory();
        string basePath = directory.Write("Foo.resx", "<root />");
        directory.Write("Foo.zh.resx", "<root />");

        CheckContext context = Build([], [Fixture.Resource($"{ns}.Foo", [], absolutePath: basePath)]);

        Assert.That(Run<MissingZhChecker>(context), Is.Empty);
    }

    [Test]
    public void L10N007_reports_keys_that_are_not_lowercase_snake_case()
    {
        CheckContext context = Build(
            [],
            [Fixture.Resource($"{ns}.Foo", ["Timing_based_colouring", "buttons.authorise", "draw_the_world"])]);

        List<Diagnostic> diagnostics = Run<KeyNamingChecker>(context);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Key, Is.EqualTo("Timing_based_colouring"));
    }

    [Test]
    public void L10N008_reports_a_class_with_no_resource_at_all()
    {
        CheckContext context = Build(
            [Fixture.Class("ScreenshotManagerStrings", $"{ns}.ScreenshotManager", [Fixture.Member("A", "a")])],
            [Fixture.Resource($"{ns}.Common", ["back"])]);

        List<Diagnostic> diagnostics = Run<PureCodeClassChecker>(context);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void L10N009_reports_a_resource_that_no_class_claims()
    {
        CheckContext context = Build([], [Fixture.Resource($"{ns}.Foo", ["a"])]);

        Assert.That(Run<OrphanResxFileChecker>(context), Has.Count.EqualTo(1));
    }

    [Test]
    public void L10N010_reports_two_files_deriving_the_same_resource_name()
    {
        CheckContext context = Build(
            [],
            [
                Fixture.Resource($"{ns}.Foo", ["a"], displayPath: "A.resx"),
                Fixture.Resource($"{ns}.Foo", ["a"], displayPath: "B.resx"),
            ]);

        List<Diagnostic> diagnostics = Run<DuplicateResourceNameChecker>(context);

        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].File, Is.EqualTo("B.resx"));
    }

    private static CheckContext Build(
        IReadOnlyList<LocalisationClass> classes,
        IReadOnlyList<ResourceFile> resources)
    {
        var workspace = new LoadedWorkspace
        {
            OsuRoot = ".",
            ResourcesRoot = ".",
            Classes = classes,
            Resources = resources,
            SkippedStringsFiles = 0,
        };

        return new CheckContext
        {
            Options = new CheckerOptions
            {
                DisabledRules = new HashSet<string>(StringComparer.Ordinal),
                SeverityOverrides = new Dictionary<string, Severity>(StringComparer.Ordinal),
            },
            Workspace = workspace,
            Matches = LocalisationMatcher.Match(classes, resources),
        };
    }

    private static List<Diagnostic> Run<TChecker>(CheckContext context)
        where TChecker : IChecker, new()
        => new TChecker().Run(context).ToList();
}
