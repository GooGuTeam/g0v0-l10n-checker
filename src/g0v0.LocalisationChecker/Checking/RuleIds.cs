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

    /// <summary>Human-readable titles, used as the <c>title=</c> of a GitHub annotation.</summary>
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
}
