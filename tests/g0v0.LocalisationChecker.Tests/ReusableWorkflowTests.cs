// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Tests;

/// <summary>
/// The reusable workflow is the one piece of this repository that no unit test can run — GitHub runs
/// it. These assertions pin the few facts its two callers depend on, because each of them can be
/// undone by an edit that still looks perfectly reasonable in review, and the damage is silent: the
/// check stays green while the alerts land in the wrong repository.
/// </summary>
[TestFixture]
public class ReusableWorkflowTests
{
    private const string workflowPath = ".github/workflows/localisation-check.yml";

    private static string Workflow()
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, workflowPath)))
            directory = directory.Parent;

        Assert.That(directory, Is.Not.Null, $"could not find {workflowPath} above the test directory");

        return File.ReadAllText(Path.Combine(directory!.FullName, workflowPath));
    }

    [Test]
    public void The_sarif_side_is_an_input_rather_than_a_constant()
    {
        string workflow = Workflow();

        Assert.That(workflow, Does.Contain("--sarif-source \"${{ inputs.sarif-source }}\""),
            "the run step has to forward the sarif-source input");

        Assert.That(workflow, Does.Not.Contain("--sarif-source osu"),
            "a hard-coded side would publish one repository's file paths into the other repository");

        Assert.That(workflow, Does.Contain("default: osu"),
            "the osu side has to stay the default, so the osu! repository's call is unaffected");
    }

    [Test]
    public void A_caller_holding_the_resx_files_has_to_name_the_osu_side()
    {
        string workflow = Workflow();

        int start = workflow.IndexOf("Refuse to cross-check a repository against itself", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0),
            "the guard against checking the resources repository against itself is gone");

        // The step body: the env mapping and the test it runs.
        string guard = workflow[start..Math.Min(workflow.Length, start + 600)];

        Assert.That(guard, Does.Contain("github.repository"), "the guard has to look at who is calling");
        Assert.That(guard, Does.Contain("inputs.resources-repository"),
            "the guard has to compare the caller against the repository holding the resx files");
    }

    [Test]
    public void The_upload_stays_best_effort_and_tolerates_a_fork()
    {
        string workflow = Workflow();

        Assert.That(workflow, Does.Contain("upload-sarif@"));

        Assert.That(workflow, Does.Contain("continue-on-error: true"),
            "a rejected upload must not fail the check; the annotations are the gate");

        Assert.That(workflow, Does.Contain("github.event.pull_request.head.repo.full_name == github.repository"),
            "a pull request from a fork has a read-only token that permissions cannot widen");
    }

    [Test]
    public void The_resources_ref_is_derived_from_the_csproj_outside_a_pull_request()
    {
        string workflow = Workflow();
        string step = Step(workflow, "Resolve the resources ref");

        Assert.That(step, Does.Contain("if: ${{ inputs.resources-ref == '' }}"),
            "an explicit ref has to win: the resources repository depends on passing the commit under test");

        Assert.That(step, Does.Contain("G0V0ResourcesVersion"),
            "the derivation has to read the property the build itself consumes");

        Assert.That(step, Does.Contain("-g0v0"),
            "the tag is v<version>-g0v0; a bare version is not a ref and fails the checkout");

        // And it has to be read through the osu checkout's directory. The checkouts put the two
        // repositories side by side in the workspace (`path: osu`, `path: resources`), so a path
        // relative to the *repository* root reads nothing and stops the run before the checker
        // starts — which is what happened the first time this step was written. Tie it to the
        // directory the checker is actually pointed at rather than to a spelling of the path.
        string osu = OsuArgument(workflow);

        Assert.That(step, Does.Contain($"{osu}/osu.Game/osu.Game.csproj"),
            $"the csproj has to be read under '{osu}', the tree the checker is pointed at");

        // Order is the whole trick: the property lives in the osu! side's working tree, and the
        // resources checkout is what consumes the answer.
        int osuSide = workflow.IndexOf("Check out the configured osu! repository", StringComparison.Ordinal);
        int resolve = workflow.IndexOf("Resolve the resources ref", StringComparison.Ordinal);
        int resources = workflow.IndexOf("Check out the resources repository", StringComparison.Ordinal);

        Assert.That(osuSide, Is.LessThan(resolve), "the csproj has to be on disk before it can be read");
        Assert.That(resolve, Is.LessThan(resources), "the ref has to be resolved before the checkout that needs it");

        Assert.That(workflow, Does.Contain("ref: ${{ inputs.resources-ref || steps.resources.outputs.ref }}"),
            "the resources checkout has to fall back to the resolved ref rather than to the default branch");
    }

    [Test]
    public void A_pull_request_is_never_pinned_to_a_released_tag()
    {
        string workflow = Workflow();
        string step = Step(workflow, "Resolve the resources ref");

        int eventTest = step.IndexOf("\"pull_request\"", StringComparison.Ordinal);
        int derivation = step.IndexOf("G0V0ResourcesVersion", StringComparison.Ordinal);

        Assert.That(eventTest, Is.GreaterThanOrEqualTo(0),
            "the resolve step no longer tells a pull request apart from the other events");
        Assert.That(eventTest, Is.LessThan(derivation),
            "a pull request is answered before the derivation is reached: it is the one case that must not be "
            + "checked against the last release, because the branch under review normally needs resources that "
            + "have not been released yet");

        // Both halves matter. The `elif` is what stops a listing that failed from falling through to
        // the tag below, which would quietly restore the old behaviour on exactly the run — a pull
        // request — that this exists to change.
        Assert.That(step, Does.Contain("elif [ -z \"$ref\" ]"),
            "the derivation has to be the else-branch of the pull request case, or an unreachable "
            + "resources repository silently pins a pull request to a release");
    }

    [Test]
    public void A_pull_request_falls_back_to_the_newest_resources()
    {
        string workflow = Workflow();
        string step = Step(workflow, "Resolve the resources ref");

        Assert.That(step, Does.Contain("git ls-remote"),
            "the step has to look at the repository's refs to answer anything at all");

        Assert.That(step, Does.Contain("--symref"),
            "without the symref line the default branch cannot be named, and naming it in the workflow "
            + "instead would break the day the repository is renamed or its default branch changes");

        Assert.That(step, Does.Contain("$2 == \"HEAD\""),
            "the commit the default branch is on has to be read, so that the summary and the checkout "
            + "cannot end up naming two different revisions");

        Assert.That(step, Does.Contain("refs/heads/"),
            "the branch has to come out of the listing, not out of a literal in this file");
    }

    [Test]
    public void A_pull_request_description_can_name_the_resources()
    {
        string workflow = Workflow();
        string step = Step(workflow, "Resolve the resources ref");

        Assert.That(step, Does.Contain("PR_BODY: ${{ github.event.pull_request.body }}"),
            "the description has to reach the step through the environment");

        int script = step.IndexOf("run: |", StringComparison.Ordinal);
        Assert.That(script, Is.GreaterThanOrEqualTo(0));

        string shell = step[script..];

        // The one assertion here that is about safety rather than behaviour: a pull request
        // description is text anyone can write, so it may never be interpolated into the script the
        // shell reads. Everything the step needs arrives as an environment variable.
        Assert.That(shell, Does.Not.Contain("${{"),
            "nothing from the event may be interpolated into the shell script");

        Assert.That(shell, Does.Contain("resources-ref[[:space:]]*:"),
            "the `Resources-Ref:` line the documentation promises has to be the one that is read");

        int description = shell.IndexOf("PR_BODY", StringComparison.Ordinal);
        int fallback = shell.IndexOf("\"pull_request\"", StringComparison.Ordinal);

        Assert.That(description, Is.GreaterThanOrEqualTo(0), "the description is never read");
        Assert.That(description, Is.LessThan(fallback),
            "a description line outranks the default, so it has to be read before the default is chosen");
    }

    [Test]
    public void The_summary_names_the_resources_that_were_compared()
    {
        string workflow = Workflow();
        string summary = Step(workflow, "name: Summarise");
        string step = Step(workflow, "Resolve the resources ref");

        Assert.That(summary, Does.Contain("RESOURCES_REF: ${{ inputs.resources-ref || steps.resources.outputs.label }}"),
            "the summary has to report the ref the checkout actually used, not the one the caller asked for");

        Assert.That(summary, Does.Contain("RESOURCES_SOURCE: ${{ steps.resources.outputs.source }}"),
            "the summary has to say where that ref came from: with three sources, the pair alone is not "
            + "enough to tell whether the check looked at a release or at a branch");

        // The two outputs the checkout and the summary each read have to be the ones the step writes.
        Assert.That(step, Does.Contain("echo \"ref=$ref\" >> \"$GITHUB_OUTPUT\""),
            "the resources checkout reads steps.resources.outputs.ref");
        Assert.That(step, Does.Contain("echo \"label=$label\" >> \"$GITHUB_OUTPUT\""),
            "the summary reads steps.resources.outputs.label");

        Assert.That(summary, Does.Contain("Resources:"),
            "a cross-repository check that does not say which two revisions it compared cannot be triaged");
    }

    private static string Step(string workflow, string marker)
    {
        int start = workflow.IndexOf(marker, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"the step matched by '{marker}' is gone");

        int end = workflow.IndexOf("\n      - ", start, StringComparison.Ordinal);

        return workflow[start..(end < 0 ? workflow.Length : end)];
    }

    /// <summary>
    /// The directory the checker is told to read the osu! side from. Anything else that reads a file
    /// out of that tree has to go through the same directory, or it reads the wrong place — the
    /// checkouts put the repositories side by side in the workspace rather than in the calling
    /// repository's root.
    /// </summary>
    private static string OsuArgument(string workflow)
    {
        string run = Step(workflow, "name: Run the checker");

        int index = run.IndexOf("--osu ", StringComparison.Ordinal);
        Assert.That(index, Is.GreaterThanOrEqualTo(0), "the checker is no longer told where the osu! side is");

        return run[(index + "--osu ".Length)..].Split(' ', '\n', '\r', '\\')[0];
    }
}
