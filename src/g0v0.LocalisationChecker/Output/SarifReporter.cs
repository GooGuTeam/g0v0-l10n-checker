// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using g0v0.LocalisationChecker.Checking;
using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Output;

/// <summary>
/// SARIF 2.1.0, the only file format GitHub code scanning accepts. Uploading the report puts every
/// finding in the repository's Security tab, where alerts have a lifecycle (open, dismissed, fixed)
/// instead of only existing for the duration of one CI run.
///
/// Two things are deliberately different from the console reporters:
///
/// * <b>One repository per report.</b> <c>artifactLocation.uri</c> is relative to the root of the
///   repository being scanned, so a single document cannot describe both sides of the cross-check.
///   <see cref="DiagnosticSource.Osu"/> and <see cref="DiagnosticSource.Resources"/> therefore have
///   to be uploaded as two separate files, and a diagnostic from an <c>--extra-resx</c> folder is
///   never reported at all: it belongs to no repository.
/// * <b>The baseline does not apply.</b> Code scanning tracks alert state itself, so hiding the
///   known issues would defeat the point of putting them in a list you can triage. The baseline
///   stays what it always was for CI: the merge gate.
/// </summary>
internal sealed class SarifReporter : IReporter
{
    private const string schema = "https://json.schemastore.org/sarif-2.1.0.json";
    private const string sarifVersion = "2.1.0";

    /// <summary>
    /// The repository this tool lives in. It is spelled out because the slug deliberately does not
    /// match <see cref="VersionInfo.Product"/> (see the README), so it cannot be derived from it.
    /// </summary>
    private const string repositoryUrl = "https://github.com/GooGuTeam/g0v0-l10n-checker";

    /// <summary>
    /// How many of the 64 SHA-256 hex digits go into a fingerprint. The reference implementation in
    /// <c>codeql-action</c> emits 16, so the same length is used here.
    /// </summary>
    private const int fingerprintLength = 16;

    private static readonly JsonSerializerOptions serialiserOptions = new()
    {
        WriteIndented = true,
        // Alert messages quote keys, prefixes and file paths, and this document is never rendered as
        // HTML, so escaping every apostrophe as \u0027 would only make stored reports unreadable.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly DiagnosticSource source;

    /// <param name="source">
    /// Which side of the cross-check this report describes. Its file paths are the ones that resolve
    /// inside the repository being scanned.
    /// </param>
    public SarifReporter(DiagnosticSource source) => this.source = source;

    public void Report(IReadOnlyList<Diagnostic> diagnostics, ReportSummary summary, TextWriter writer)
    {
        // summary counts what the console reporters print; a SARIF file carries alerts only.

        List<Diagnostic> selected = diagnostics.Where(diagnostic => diagnostic.Source == source).ToList();
        selected.Sort(Compare);

        JsonArray rules = new();
        var ruleIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        // The registry has no particular order, but a report reads better with the rules in id order,
        // and every result carries the resulting index.
        foreach (IChecker checker in CheckerRegistry.All.OrderBy(checker => checker.RuleId, StringComparer.Ordinal))
        {
            ruleIndex[checker.RuleId] = rules.Count;
            rules.Add(buildRule(checker));
        }

        var fingerprintsSeen = new Dictionary<string, int>(StringComparer.Ordinal);
        JsonArray results = new();

        foreach (Diagnostic diagnostic in selected)
        {
            string digest = fingerprint(diagnostic);
            int ordinal = fingerprintsSeen.TryGetValue(digest, out int previous) ? previous + 1 : 1;
            fingerprintsSeen[digest] = ordinal;

            results.Add(buildResult(diagnostic, ruleIndex, $"{digest}:{ordinal}"));
        }

        var run = new JsonObject
        {
            ["tool"] = new JsonObject
            {
                ["driver"] = new JsonObject
                {
                    ["name"] = VersionInfo.Product,
                    ["version"] = VersionInfo.Version,
                    ["informationUri"] = repositoryUrl,
                    ["rules"] = rules,
                },
            },
            ["results"] = results,
        };

        var root = new JsonObject
        {
            ["$schema"] = schema,
            ["version"] = sarifVersion,
            ["runs"] = new JsonArray { run },
        };

        writer.WriteLine(root.ToJsonString(serialiserOptions));
    }

    private static JsonObject buildRule(IChecker checker) => new()
    {
        ["id"] = checker.RuleId,
        ["shortDescription"] = new JsonObject { ["text"] = RuleIds.Title(checker.RuleId) },
        ["fullDescription"] = new JsonObject { ["text"] = RuleIds.Description(checker.RuleId) },
        ["help"] = new JsonObject { ["text"] = RuleIds.Help(checker.RuleId) },
        ["helpUri"] = RuleIds.DocumentationUrl,
        ["defaultConfiguration"] = new JsonObject { ["level"] = level(checker.DefaultSeverity) },
        ["properties"] = new JsonObject
        {
            // Code scanning decides what to show by default from these two, and every rule here is a
            // deterministic cross-check rather than a heuristic, so all of them qualify.
            ["precision"] = "high",
            ["problem.severity"] = problemSeverity(checker.DefaultSeverity),
        },
    };

    private static JsonObject buildResult(Diagnostic diagnostic, Dictionary<string, int> ruleIndex, string fingerprint)
    {
        // Diagnostics are reported per member, not per column, and a file level finding has no line
        // at all. SARIF needs both lines and columns to be at least 1, and it allows an empty region
        // (endColumn equal to startColumn), which is the honest description of "this line, column
        // unknown".
        int line = Math.Max(1, diagnostic.Line);

        var result = new JsonObject
        {
            ["ruleId"] = diagnostic.RuleId,
            ["level"] = level(diagnostic.Severity),
            ["message"] = new JsonObject { ["text"] = diagnostic.Message },
            ["locations"] = new JsonArray
            {
                new JsonObject
                {
                    ["physicalLocation"] = new JsonObject
                    {
                        ["artifactLocation"] = new JsonObject { ["uri"] = diagnostic.File },
                        ["region"] = new JsonObject
                        {
                            ["startLine"] = line,
                            ["startColumn"] = 1,
                            ["endLine"] = line,
                            ["endColumn"] = 1,
                        },
                    },
                },
            },
            ["partialFingerprints"] = new JsonObject { ["primaryLocationLineHash"] = fingerprint },
        };

        // Only rules that ran can be indexed; a disabled rule still appears in the descriptor list.
        if (ruleIndex.TryGetValue(diagnostic.RuleId, out int index))
            result["ruleIndex"] = index;

        return result;
    }

    /// <summary>
    /// A stable alert identity. GitHub uses <c>primaryLocationLineHash</c> to decide which alerts a
    /// run created, kept and closed, and it falls back to hashing the file it finds at the reported
    /// path — which is the wrong file for this tool, because the path belongs to the scanned
    /// repository rather than the one the checker runs in. Hashing the same "rule|source|file|key"
    /// tuple the baseline keys on keeps an alert stable across line moves and file reshuffles, and
    /// the ordinal disambiguates the (never expected) case of two identical tuples.
    /// </summary>
    private static string fingerprint(Diagnostic diagnostic)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(diagnostic.BaselineKey));

        return Convert.ToHexStringLower(digest)[..fingerprintLength];
    }

    private static int Compare(Diagnostic left, Diagnostic right)
    {
        int byFile = string.CompareOrdinal(left.File, right.File);
        if (byFile != 0)
            return byFile;

        int byLine = left.Line.CompareTo(right.Line);
        if (byLine != 0)
            return byLine;

        int byRule = string.CompareOrdinal(left.RuleId, right.RuleId);
        if (byRule != 0)
            return byRule;

        return string.CompareOrdinal(left.Key, right.Key);
    }

    private static string level(Severity severity) => severity switch
    {
        Severity.Error => "error",
        Severity.Warning => "warning",
        _ => "note",
    };

    private static string problemSeverity(Severity severity) => severity switch
    {
        Severity.Error => "error",
        Severity.Warning => "warning",
        _ => "recommendation",
    };
}
