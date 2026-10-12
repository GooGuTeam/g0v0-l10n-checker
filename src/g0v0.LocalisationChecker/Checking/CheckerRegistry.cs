// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Checking.Checkers;

namespace g0v0.LocalisationChecker.Checking;

/// <summary>The set of rules the tool runs, in reporting order.</summary>
internal static class CheckerRegistry
{
    public static IReadOnlyList<IChecker> All { get; } =
    [
        new PrefixMismatchChecker(),
        new MissingResourceChecker(),
        new DuplicateKeyChecker(),
        new DocFallbackMismatchChecker(),
        new OrphanResourceKeyChecker(),
        new OrphanResxFileChecker(),
        new MissingZhChecker(),
        new KeyNamingChecker(),
        new PureCodeClassChecker(),
        new DuplicateResourceNameChecker(),
    ];
}
