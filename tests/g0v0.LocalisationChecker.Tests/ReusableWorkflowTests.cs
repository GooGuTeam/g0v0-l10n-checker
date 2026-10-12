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
    public void The_resources_ref_is_derived_from_the_csproj_when_a_caller_names_none()
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
    public void The_summary_names_the_resources_that_were_compared()
    {
        string workflow = Workflow();
        string summary = Step(workflow, "name: Summarise");

        Assert.That(summary, Does.Contain("RESOURCES_REF: ${{ inputs.resources-ref || steps.resources.outputs.ref }}"),
            "the summary has to report the ref the checkout actually used, not the one the caller asked for");

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
