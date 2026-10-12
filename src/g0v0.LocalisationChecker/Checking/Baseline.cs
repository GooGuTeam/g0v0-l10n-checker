// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.Json;
using System.Text.Json.Nodes;
using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking;

/// <summary>Thrown when a baseline file is missing or malformed. Maps to exit code 3.</summary>
internal sealed class BaselineException : Exception
{
    public BaselineException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// A committed list of diagnostics that are accepted as-is. Anything in here is suppressed, so CI
/// only fails on newly introduced problems - which is what makes the checker usable on a repository
/// that already has known issues.
/// </summary>
internal sealed class Baseline
{
    private readonly HashSet<string> keys;

    private Baseline(HashSet<string> keys) => this.keys = keys;

    public static Baseline Empty { get; } = new(new HashSet<string>(StringComparer.Ordinal));

    public int Count => keys.Count;

    public bool Contains(Diagnostic diagnostic) => keys.Contains(diagnostic.BaselineKey);

    public static Baseline Load(string path)
    {
        if (!File.Exists(path))
            throw new BaselineException($"baseline file does not exist: {path}");

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            throw new BaselineException($"baseline file is not valid JSON: {path} ({ex.Message})");
        }

        if (node is not JsonObject root)
            throw new BaselineException($"baseline file must contain a JSON object: {path}");

        var loaded = new HashSet<string>(StringComparer.Ordinal);

        if (root["entries"] is JsonArray entries)
        {
            foreach (JsonNode? entry in entries)
            {
                if (entry is not JsonObject obj)
                    continue;

                string? rule = obj["rule"]?.GetValue<string>();
                string? file = obj["file"]?.GetValue<string>();
                if (rule is null || file is null)
                    continue;

                string sourceText = obj["source"]?.GetValue<string>() ?? nameof(DiagnosticSource.Osu);
                if (!Enum.TryParse(sourceText, ignoreCase: true, out DiagnosticSource source))
                    source = DiagnosticSource.Osu;

                loaded.Add(Diagnostic.BuildKey(rule, source, file, obj["key"]?.GetValue<string>()));
            }
        }

        return new Baseline(loaded);
    }

    public static void Save(string path, IEnumerable<Diagnostic> diagnostics)
    {
        var entries = new JsonArray();

        foreach (Diagnostic diagnostic in diagnostics
                     .OrderBy(d => d.RuleId, StringComparer.Ordinal)
                     .ThenBy(d => d.Source)
                     .ThenBy(d => d.File, StringComparer.Ordinal)
                     .ThenBy(d => d.Key, StringComparer.Ordinal))
        {
            var entry = new JsonObject
            {
                ["rule"] = diagnostic.RuleId,
                ["source"] = diagnostic.Source.ToString(),
                ["file"] = diagnostic.File,
            };

            if (diagnostic.Key is not null)
                entry["key"] = diagnostic.Key;

            entries.Add(entry);
        }

        var root = new JsonObject
        {
            ["version"] = 1,
            ["entries"] = entries,
        };

        string json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, json + Environment.NewLine);
    }
}
