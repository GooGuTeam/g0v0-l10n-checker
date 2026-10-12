// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Checking;
using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Output;

namespace g0v0.LocalisationChecker.Cli;

/// <summary>
/// A hand-written argument parser. Keeping it in-house avoids a dependency for what is a dozen
/// switches, and lets the tool accept the exact shapes the CI workflow needs.
/// </summary>
internal static class CliParser
{
    public static CliOptions Parse(IReadOnlyList<string> args)
    {
        var options = new CliOptions();

        int index = 0;
        while (index < args.Count)
        {
            string argument = args[index];

            if (argument.Length == 0 || argument[0] != '-')
                throw new CliException($"unexpected argument '{argument}'");

            string name = argument;
            string? inlineValue = null;

            int equals = argument.IndexOf('=');
            if (equals >= 0)
            {
                name = argument[..equals];
                inlineValue = argument[(equals + 1)..];
            }

            switch (name)
            {
                case "--osu":
                case "-o":
                    options.OsuRoot = TakeValue(args, ref index, name, inlineValue);
                    break;

                case "--resources":
                case "-r":
                    options.ResourcesRoot = TakeValue(args, ref index, name, inlineValue);
                    break;

                case "--extra-resx":
                case "-x":
                    options.ExtraResx.Add(ParseExtraResx(TakeValue(args, ref index, name, inlineValue)));
                    break;

                case "--color":
                case "-c":
                    options.Color = ParseColorMode(TakeValue(args, ref index, name, inlineValue));
                    break;

                case "--format":
                case "-f":
                    options.Format = ParseOutputFormat(TakeValue(args, ref index, name, inlineValue));
                    break;

                case "--min-severity":
                    options.MinSeverity = ParseSeverity(TakeValue(args, ref index, name, inlineValue), name);
                    break;

                case "--fail-on":
                    options.FailOn = ParseFailOn(TakeValue(args, ref index, name, inlineValue));
                    break;

                case "--ignore-rule":
                    options.IgnoreRules.Add(CanonicalRuleId(TakeValue(args, ref index, name, inlineValue)));
                    break;

                case "--severity":
                    AddSeverityOverride(options, TakeValue(args, ref index, name, inlineValue));
                    break;

                case "--baseline":
                    options.BaselinePath = TakeValue(args, ref index, name, inlineValue);
                    break;

                case "--no-baseline":
                    RejectInlineValue(name, inlineValue);
                    options.NoBaseline = true;
                    break;

                case "--write-baseline":
                    options.WriteBaselinePath = TakeValue(args, ref index, name, inlineValue);
                    break;

                case "--json-report":
                    options.JsonReportPath = TakeValue(args, ref index, name, inlineValue);
                    break;

                case "--sarif-report":
                    options.SarifReportPath = TakeValue(args, ref index, name, inlineValue);
                    break;

                case "--sarif-source":
                    options.SarifSource = ParseSarifSource(TakeValue(args, ref index, name, inlineValue));
                    break;

                case "--no-summary":
                    RejectInlineValue(name, inlineValue);
                    options.NoSummary = true;
                    break;

                case "--quiet":
                case "-q":
                    RejectInlineValue(name, inlineValue);
                    options.MinSeverity = Severity.Error;
                    options.NoSummary = true;
                    break;

                case "--help":
                case "-h":
                    RejectInlineValue(name, inlineValue);
                    options.ShowHelp = true;
                    break;

                case "--version":
                    RejectInlineValue(name, inlineValue);
                    options.ShowVersion = true;
                    break;

                default:
                    throw new CliException($"unknown option '{name}'");
            }

            index++;
        }

        return options;
    }

    /// <summary>Splits <c>DIR[=NAMESPACE]</c>. The form <c>--extra-resx=DIR=NS</c> also works.</summary>
    private static ExtraResxFolder ParseExtraResx(string value)
    {
        int separator = value.IndexOf('=');

        string path = separator < 0 ? value : value[..separator];
        string rootNamespace = separator < 0 ? string.Empty : value[(separator + 1)..];

        if (path.Length == 0)
            throw new CliException("--extra-resx requires a non-empty directory");

        return new ExtraResxFolder(path, rootNamespace, path);
    }

    private static string TakeValue(IReadOnlyList<string> args, ref int index, string name, string? inlineValue)
    {
        if (inlineValue is not null)
            return inlineValue;

        if (index + 1 >= args.Count)
            throw new CliException($"option '{name}' requires a value");

        index++;
        return args[index];
    }

    private static void RejectInlineValue(string name, string? inlineValue)
    {
        if (inlineValue is not null)
            throw new CliException($"option '{name}' does not take a value");
    }

    private static ColorMode ParseColorMode(string value) => value.ToLowerInvariant() switch
    {
        "auto" => ColorMode.Auto,
        "always" => ColorMode.Always,
        "never" => ColorMode.Never,
        _ => throw new CliException($"invalid value '{value}' for --color (expected auto, always or never)"),
    };

    private static OutputFormat ParseOutputFormat(string value) => value.ToLowerInvariant() switch
    {
        "auto" => OutputFormat.Auto,
        "human" => OutputFormat.Human,
        "github" => OutputFormat.GitHub,
        "json" => OutputFormat.Json,
        _ => throw new CliException($"invalid value '{value}' for --format (expected auto, human, github or json)"),
    };

    private static Severity ParseSeverity(string value, string name) => value.ToLowerInvariant() switch
    {
        "info" => Severity.Info,
        "warning" => Severity.Warning,
        "error" => Severity.Error,
        _ => throw new CliException($"invalid value '{value}' for {name} (expected info, warning or error)"),
    };

    /// <summary>
    /// Only the two real repositories are addressable: an <c>--extra-resx</c> folder is not a
    /// repository, so a path relative to its root would resolve nowhere.
    /// </summary>
    private static DiagnosticSource ParseSarifSource(string value) => value.ToLowerInvariant() switch
    {
        "osu" => DiagnosticSource.Osu,
        "resources" => DiagnosticSource.Resources,
        _ => throw new CliException($"invalid value '{value}' for --sarif-source (expected osu or resources)"),
    };

    private static FailOn ParseFailOn(string value) => value.ToLowerInvariant() switch
    {
        "never" => FailOn.Never,
        "error" => FailOn.Error,
        "warning" => FailOn.Warning,
        _ => throw new CliException($"invalid value '{value}' for --fail-on (expected never, error or warning)"),
    };

    private static void AddSeverityOverride(CliOptions options, string value)
    {
        int separator = value.IndexOf('=');
        if (separator < 0)
            throw new CliException("--severity expects RULE=LEVEL, for example --severity L10N008=error");

        string rule = CanonicalRuleId(value[..separator]);
        options.SeverityOverrides[rule] = ParseSeverity(value[(separator + 1)..], "--severity");
    }

    /// <summary>Maps a user-supplied rule id onto its canonical casing so lookups always hit.</summary>
    private static string CanonicalRuleId(string value)
    {
        string? match = RuleIds.All.FirstOrDefault(rule => string.Equals(rule, value, StringComparison.OrdinalIgnoreCase));

        return match ?? throw new CliException($"unknown rule id '{value}'");
    }
}
