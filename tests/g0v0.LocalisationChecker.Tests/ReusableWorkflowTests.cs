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
}
