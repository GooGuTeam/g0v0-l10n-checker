// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Output;

/// <summary>
/// Machine-readable output. The schema carries a version number because it is a contract for
/// downstream tooling rather than a debugging aid.
/// </summary>
internal sealed class JsonReporter : IReporter
{
    private static readonly JsonSerializerOptions serialiserOptions = new()
    {
        WriteIndented = true,
        // JsonSerializerOptions.NewLine defaults to Environment.NewLine, which would make the same
        // report CRLF on Windows and LF on CI. Reports are diffed and read back by other tools, so
        // the bytes must not depend on the machine that wrote them.
        NewLine = "\n",
        // Translation text is full of apostrophes and quotes; escaping them as \u0027 would make the
        // report unreadable for no benefit, since this is never injected into an HTML document.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string osuRoot;
    private readonly string resourcesRoot;

    public JsonReporter(string osuRoot, string resourcesRoot)
    {
        this.osuRoot = osuRoot;
        this.resourcesRoot = resourcesRoot;
    }

    public void Report(IReadOnlyList<Diagnostic> diagnostics, ReportSummary summary, TextWriter writer)
    {
        // A stored report is only useful if it says which build produced it, so the tool's own
        // identity travels with it instead of having to be inferred from the surrounding pipeline.
        var tool = new JsonObject
        {
            ["name"] = VersionInfo.Product,
            ["version"] = VersionInfo.Version,
        };

        if (VersionInfo.Revision is not null)
            tool["revision"] = VersionInfo.Revision;

        var array = new JsonArray();

        foreach (Diagnostic diagnostic in diagnostics)
        {
            var entry = new JsonObject
            {
                ["rule"] = diagnostic.RuleId,
                ["severity"] = diagnostic.Severity.ToString().ToLowerInvariant(),
                ["source"] = diagnostic.Source.ToString().ToLowerInvariant(),
                ["file"] = diagnostic.File,
                ["line"] = diagnostic.Line,
                ["message"] = diagnostic.Message,
            };

            if (diagnostic.Key is not null)
                entry["key"] = diagnostic.Key;

            if (diagnostic.Suggestion is not null)
                entry["suggestion"] = diagnostic.Suggestion;

            array.Add(entry);
        }

        var root = new JsonObject
        {
            ["version"] = 1,
            ["tool"] = tool,
            ["osuRoot"] = osuRoot,
            ["resourcesRoot"] = resourcesRoot,
            ["diagnostics"] = array,
            ["summary"] = new JsonObject
            {
                ["classes"] = summary.Classes,
                ["resources"] = summary.Resources,
                ["skippedStringsFiles"] = summary.SkippedStringsFiles,
                ["error"] = summary.Errors,
                ["warning"] = summary.Warnings,
                ["info"] = summary.Infos,
                ["suppressed"] = summary.Suppressed,
            },
        };

        writer.WriteLine(root.ToJsonString(serialiserOptions));
    }
}
