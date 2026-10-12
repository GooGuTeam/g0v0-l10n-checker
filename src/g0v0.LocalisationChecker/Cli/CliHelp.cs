// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Cli;

/// <summary>
/// Usage text. Kept beside the parser so the two cannot drift, and interpolated with the product name
/// from the assembly metadata so the two spellings cannot drift either.
/// </summary>
internal static class CliHelp
{
    public static string Text { get; } = $$"""
        {{VersionInfo.Product}} - cross-checks osu! localisation classes against
                                   osu-resources .resx files.

        Usage:
          {{VersionInfo.Product}} [options]

        Inputs:
          -o, --osu <dir>              osu! repository root. Default: D:/Projects/osu
                                       (or $G0V0_OSU_PATH)
          -r, --resources <dir>        osu-resources repository root. Default: D:/Projects/osu-resources
                                       (or $G0V0_RESOURCES_PATH)
          -x, --extra-resx <DIR[=NS]>  Additional resx folder to include, repeatable. The optional
                                       NS is the project's root namespace.
                                       Example: -x "Custom/Localisation=Custom.Localisation"

        Output:
          -c, --color <mode>           auto | always | never. Default: auto
          -f, --format <mode>          auto | human | github | json. Default: auto
                                       (github is selected automatically under GitHub Actions)
              --min-severity <level>   info | warning | error. Default: info
              --json-report <file>     Also write the JSON document here, whatever --format is
              --sarif-report <file>    Also write a SARIF 2.1.0 report here, for GitHub code
                                       scanning. Ignores the baseline on purpose: the Security tab
                                       tracks alert state itself, so the known issues are uploaded
                                       too and triaged there
              --sarif-source <side>    osu | resources. Which repository the SARIF file describes.
                                       Default: osu. Its paths are relative to that repository's
                                       root, so one report never covers both sides
          -q, --quiet                  Shorthand for --min-severity error --no-summary
              --no-summary             Hide the trailing totals

        Failing the run:
              --fail-on <level>        never | error | warning. Default: error

        Filtering:
              --ignore-rule <RULE>     Disable a rule, repeatable. Example: --ignore-rule L10N008
              --severity <R=LEVEL>     Override a rule's severity, repeatable.
                                       Example: --severity L10N008=error

        Baselines:
              --baseline <file>        Baseline to read. Default: .localisation-baseline.json in
                                       the working directory, when it exists
              --no-baseline            Ignore baselines entirely
              --write-baseline <file>  Write the current diagnostics (warning and above) and exit

        Misc:
          -h, --help                   Show this help
              --version                Show the tool version (read from the assembly metadata)

        Rules:
          L10N001 error    a member's key is not defined in its resource file
          L10N002 warning  a resource key has no matching localisation member
          L10N003 error    the prefix constant does not match any resource name
          L10N004 error    two members of the same class use the same key
          L10N005 error    a member's XML doc text and its fallback text disagree
          L10N006 warning  a base resource has no paired .zh.resx
          L10N007 warning  a resource key is not lowercase snake_case
          L10N008 warning  a localisation class has no resource at all
          L10N009 warning  a resource file is claimed by no localisation class
          L10N010 warning  two scanned files derive the same resource name

        Exit codes:
          0  no diagnostic at or above --fail-on
          1  diagnostics at or above --fail-on were found
          2  command line error
          3  input error (missing directory, unreadable resx, bad baseline)
        """;
}
