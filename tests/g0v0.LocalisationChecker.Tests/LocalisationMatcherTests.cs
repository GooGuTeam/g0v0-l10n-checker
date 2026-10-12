// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Checking;
using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Tests;

[TestFixture]
public class LocalisationMatcherTests
{
    private const string ns = "osu.Game.Resources.Localisation";

    [Test]
    public void Matches_on_the_prefix_even_when_the_file_names_disagree()
    {
        // HUD/ColourHitError.resx belongs to ColourHitErrorMeterStrings.cs - file names differ but
        // the prefix is correct, so no heuristic should be involved at all.
        LocalisationClass cls = Fixture.Class(
            "ColourHitErrorMeterStrings",
            $"{ns}.HUD.ColourHitError",
            [Fixture.Member("JudgementCount", "judgement_count")],
            "HUD");

        ResourceFile resource = Fixture.Resource($"{ns}.HUD.ColourHitError", ["judgement_count"], "HUD");

        MatchOutcome outcome = Match(cls, resource);

        Assert.That(outcome.Exact, Is.SameAs(resource));
        Assert.That(outcome.Suggested, Is.Null);
        Assert.That(outcome.Suspect, Is.Null);
    }

    [Test]
    public void Suggests_the_sibling_resource_when_the_prefix_dropped_a_segment()
    {
        LocalisationClass cls = Fixture.Class(
            "SpectatorListStrings",
            $"{ns}.SpectatorList",
            [Fixture.Member("SpectatorCount", "spectator_count")],
            "HUD");

        ResourceFile resource = Fixture.Resource($"{ns}.HUD.SpectatorList", ["spectator_count"], "HUD");

        MatchOutcome outcome = Match(cls, resource);

        Assert.That(outcome.Exact, Is.Null);
        Assert.That(outcome.Suggested, Is.SameAs(resource));
        Assert.That(outcome.Ambiguous, Is.False);
    }

    [Test]
    public void Reports_nothing_when_no_resource_could_be_found()
    {
        LocalisationClass cls = Fixture.Class(
            "ScreenshotManagerStrings",
            $"{ns}.ScreenshotManager",
            [Fixture.Member("UploadSuccess", "upload_success")]);

        ResourceFile unrelated = Fixture.Resource($"{ns}.Common", ["back"]);

        MatchOutcome outcome = Match(cls, unrelated);

        Assert.That(outcome.Exact, Is.Null);
        Assert.That(outcome.Suggested, Is.Null);
        Assert.That(outcome.Ambiguous, Is.False);
        Assert.That(outcome.Effective, Is.Null);
    }

    [Test]
    public void Flags_a_prefix_that_names_a_resource_defining_none_of_the_classes_keys()
    {
        LocalisationClass cls = Fixture.Class(
            "TaikoEditorStrings",
            $"{ns}.Osu.OsuEditor",
            [
                Fixture.Member("HitTool", "hit_tool"),
                Fixture.Member("DrumRollTool", "drum_roll_tool"),
                Fixture.Member("SwellTool", "swell_tool"),
            ],
            "Taiko");

        ResourceFile wrong = Fixture.Resource($"{ns}.Osu.OsuEditor", ["slider_tool"], "Osu");
        ResourceFile right = Fixture.Resource($"{ns}.Taiko.OsuEditor", ["hit_tool", "drum_roll_tool", "swell_tool"], "Taiko");

        MatchOutcome outcome = Match(cls, wrong, right);

        Assert.That(outcome.Exact, Is.SameAs(wrong));
        Assert.That(outcome.Suspect, Is.SameAs(right));
    }

    [Test]
    public void Does_not_guess_when_several_candidates_score_equally()
    {
        LocalisationClass cls = Fixture.Class(
            "FooStrings",
            $"{ns}.Bar",
            [Fixture.Member("A", "a")],
            "X");

        ResourceFile first = Fixture.Resource($"{ns}.X.Foo", ["a"], "X");
        ResourceFile second = Fixture.Resource($"{ns}.X.FooStrings", ["a"], "X");

        MatchOutcome outcome = Match(cls, first, second);

        Assert.That(outcome.Exact, Is.Null);
        Assert.That(outcome.Suggested, Is.Null);
        Assert.That(outcome.Ambiguous, Is.True);
    }

    private static MatchOutcome Match(LocalisationClass cls, params ResourceFile[] resources)
        => LocalisationMatcher.Match([cls], resources)[cls];
}
