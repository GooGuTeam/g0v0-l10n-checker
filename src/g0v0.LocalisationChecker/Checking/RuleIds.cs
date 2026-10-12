// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Checking;

/// <summary>
/// Stable identifiers for every rule the checker implements. These appear in output, in baselines
/// and in CI annotations, so they must never be renumbered.
/// </summary>
internal static class RuleIds
{
    /// <summary>A member's key is not defined in its resource file.</summary>
    public const string MissingResource = "L10N001";

    /// <summary>A resource key has no matching localisation member.</summary>
    public const string OrphanResourceKey = "L10N002";

    /// <summary>The <c>prefix</c> constant does not match any resource name.</summary>
    public const string PrefixMismatch = "L10N003";

    /// <summary>Two members of the same class use the same key.</summary>
    public const string DuplicateKeyInClass = "L10N004";

    /// <summary>A member's XML doc text and its fallback text disagree.</summary>
    public const string DocFallbackMismatch = "L10N005";

    /// <summary>A base resource has no paired <c>.zh.resx</c>.</summary>
    public const string MissingZhTranslation = "L10N006";

    /// <summary>A resource key is not lowercase snake_case.</summary>
    public const string KeyNamingViolation = "L10N007";

    /// <summary>A localisation class has no resource at all (not translatable).</summary>
    public const string PureCodeClass = "L10N008";

    /// <summary>A resource file is claimed by no localisation class.</summary>
    public const string OrphanResxFile = "L10N009";

    /// <summary>Two scanned files derive the same resource name.</summary>
    public const string DuplicateResourceName = "L10N010";

    public static readonly IReadOnlyList<string> All =
    [
        MissingResource,
        OrphanResourceKey,
        PrefixMismatch,
        DuplicateKeyInClass,
        DocFallbackMismatch,
        MissingZhTranslation,
        KeyNamingViolation,
        PureCodeClass,
        OrphanResxFile,
        DuplicateResourceName,
    ];

    /// <summary>
    /// Human-readable titles, used as the <c>title=</c> of a GitHub annotation and as the
    /// <c>shortDescription</c> of a SARIF rule.
    /// </summary>
    public static string Title(string ruleId) => ruleId switch
    {
        MissingResource => "Missing resource key",
        OrphanResourceKey => "Orphan resource key",
        PrefixMismatch => "Prefix mismatch",
        DuplicateKeyInClass => "Duplicate key in class",
        DocFallbackMismatch => "Documentation/fallback mismatch",
        MissingZhTranslation => "Missing zh translation",
        KeyNamingViolation => "Key naming violation",
        PureCodeClass => "Class has no resource",
        OrphanResxFile => "Orphan resource file",
        DuplicateResourceName => "Duplicate resource name",
        _ => ruleId,
    };

    /// <summary>
    /// One sentence per rule explaining what it means for the localisation pipeline. Used as the
    /// <c>fullDescription</c> of a SARIF rule, where GitHub shows it next to every alert.
    /// </summary>
    public static string Description(string ruleId) => ruleId switch
    {
        MissingResource =>
            "A localisation member declares a key that its resource file does not define. The runtime "
            + "lookup falls back to the C# string, so the string can never be translated.",
        OrphanResourceKey =>
            "A resource file defines a key that no localisation member declares. The translation is dead "
            + "weight, because no code path ever reads it.",
        PrefixMismatch =>
            "A class's prefix constant does not name an existing resource, so the class reads from the "
            + "wrong resource or from none at all and is permanently untranslatable.",
        DuplicateKeyInClass =>
            "Two members of one class declare the same key, so both resolve to the same resource entry "
            + "and cannot be translated independently.",
        DocFallbackMismatch =>
            "A member's XML documentation and its English fallback disagree. One of the two is stale, "
            + "and translators read the doc while players see the fallback.",
        MissingZhTranslation =>
            "A base resource file has no paired .zh.resx, so every string it defines falls back to "
            + "English for Chinese players.",
        KeyNamingViolation =>
            "A resource key is not lowercase snake_case. osu! keys are conventionally snake_case with "
            + "optional dotted groups, which is what the rest of the pipeline assumes.",
        PureCodeClass =>
            "A localisation class names no resource at all, so every string in it is permanently "
            + "untranslatable. The runtime tolerates this, which is why it is a warning.",
        OrphanResxFile =>
            "A resource file in the scanned tree is claimed by no localisation class, so its strings "
            + "are unreachable.",
        DuplicateResourceName =>
            "Two scanned files derive the same resource name, so the runtime resolves them to a single "
            + "manifest entry and the other one is never loaded.",
        _ => Title(ruleId),
    };

    /// <summary>
    /// Where the rules are documented. A SARIF rule's <c>helpUri</c> and <c>help.text</c> both point
    /// here, so the two can never disagree about where to look.
    /// </summary>
    public const string DocumentationUrl =
        "https://github.com/GooGuTeam/g0v0-l10n-checker/blob/main/README.md#rules";

    /// <summary>Long-form help for one rule, used as the <c>help.text</c> of a SARIF rule.</summary>
    public static string Help(string ruleId)
        => $"{Description(ruleId)}\n\nSee {DocumentationUrl} for what every rule does and which severity it defaults to.";
}
