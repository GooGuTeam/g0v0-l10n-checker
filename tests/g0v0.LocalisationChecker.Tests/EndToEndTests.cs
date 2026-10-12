// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Checking;
using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Workspace;

namespace g0v0.LocalisationChecker.Tests;

/// <summary>
/// Runs the whole pipeline against real checkouts. The test is skipped unless both repositories are
/// present, so it acts as a local acceptance test rather than a CI requirement.
/// </summary>
[TestFixture]
public class EndToEndTests
{
    private const string osuRoot = "D:/Projects/osu";
    private const string resourcesRoot = "D:/Projects/osu-resources";

    private LoadedWorkspace workspace = null!;
    private List<Diagnostic> diagnostics = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        if (!Directory.Exists(osuRoot) || !Directory.Exists(resourcesRoot))
            Assert.Ignore("osu / osu-resources checkouts are not present on this machine");

        workspace = WorkspaceLoader.Load(osuRoot, resourcesRoot, []);

        IReadOnlyDictionary<LocalisationClass, MatchOutcome> matches =
            LocalisationMatcher.Match(workspace.Classes, workspace.Resources);

        var context = new CheckContext
        {
            Options = new CheckerOptions
            {
                DisabledRules = new HashSet<string>(StringComparer.Ordinal),
                SeverityOverrides = new Dictionary<string, Severity>(StringComparer.Ordinal),
            },
            Workspace = workspace,
            Matches = matches,
        };

        diagnostics = CheckerRegistry.All.SelectMany(checker => checker.Run(context)).ToList();
    }

    [Test]
    public void Discovers_both_sides_of_the_localisation_pipeline()
    {
        Assert.That(workspace.Classes, Has.Count.GreaterThan(90), "localisation classes should have been found");
        Assert.That(workspace.Classes.All(cls => cls.Prefix.Length > 0), Is.True, "every class must yield a prefix");
        Assert.That(workspace.Resources.Count(r => r.InReverseScope), Is.GreaterThan(90), "game-scope resources should have been found");
        Assert.That(workspace.SkippedStringsFiles, Is.EqualTo(0), "no *Strings.cs file should lack a prefix");
    }

    [Test]
    public void Every_class_parses_at_least_one_member()
    {
        List<LocalisationClass> empty = workspace.Classes.Where(cls => cls.Members.Count == 0).ToList();

        Assert.That(empty.Select(cls => cls.DisplayPath), Is.Empty);
    }

    [Test]
    public void No_class_declares_the_same_key_twice()
    {
        Assert.That(
            Files(RuleIds.DuplicateKeyInClass),
            Is.Empty,
            "L10N004 should have no hits in this repository");
    }

    [Test]
    public void Every_doc_comment_matches_its_fallback()
    {
        Assert.That(
            Files(RuleIds.DocFallbackMismatch),
            Is.Empty,
            "L10N005 should have no hits in this repository");
    }

    [Test]
    public void A_correct_prefix_is_never_flagged_even_when_the_file_names_disagree()
    {
        // HUD/ColourHitError.resx belongs to ColourHitErrorMeterStrings.cs. Matching must go through
        // the prefix, so this class has to stay silent.
        Assert.That(
            Files(RuleIds.PrefixMismatch).Where(file => file.Contains("ColourHitErrorMeterStrings", StringComparison.Ordinal)),
            Is.Empty);
    }

    [Test]
    public void Every_prefix_mismatch_comes_with_a_suggestion()
    {
        List<Diagnostic> mismatches = diagnostics.Where(d => d.RuleId == RuleIds.PrefixMismatch).ToList();

        Assert.That(mismatches, Is.Not.Empty, "this repository is known to contain prefix mismatches");
        Assert.That(mismatches.All(d => d.Suggestion is not null), Is.True);
    }

    [Test]
    public void Detects_the_known_upstream_prefix_mismatch()
    {
        // Canary for the rule that motivated this tool. Delete this assertion if upstream fixes the
        // file, and move the case into the unit tests instead.
        Assert.That(
            Files(RuleIds.PrefixMismatch),
            Does.Contain("osu.Game/Localisation/HUD/SpectatorListStrings.cs"));
    }

    [Test]
    public void Detects_the_class_whose_prefix_points_at_a_neighbouring_ruleset()
    {
        Assert.That(
            Files(RuleIds.PrefixMismatch),
            Does.Contain("osu.Game/Localisation/Taiko/TaikoEditorStrings.cs"));
    }

    [Test]
    public void Detects_the_class_that_has_no_resource_at_all()
    {
        Assert.That(
            Files(RuleIds.PureCodeClass),
            Does.Contain("osu.Game/Localisation/ScreenshotManagerStrings.cs"));
    }

    [Test]
    public void Reports_drift_in_both_directions()
    {
        Assert.That(Files(RuleIds.MissingResource), Is.Not.Empty, "keys missing from the resources repository");
        Assert.That(Files(RuleIds.OrphanResourceKey), Is.Not.Empty, "resource keys no class declares");
    }

    private List<string> Files(string ruleId)
        => diagnostics.Where(d => d.RuleId == ruleId).Select(d => d.File).ToList();
}
