// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking;

/// <summary>
/// Ties localisation classes to resource files.
/// </summary>
/// <remarks>
/// <para>
/// The <c>prefix</c> constant is the only authoritative source of a class's resource namespace, so
/// an exact match on the derived resource name is always tried first.
/// </para>
/// <para>
/// A fallback heuristic only runs when that fails, because the repository contains real examples of
/// both kinds of surprise: <c>HUD/ColourHitError.resx</c> belongs to
/// <c>ColourHitErrorMeterStrings.cs</c> (file names disagree but the prefix is right), while
/// <c>HUD/SpectatorListStrings.cs</c> points at <c>...Localisation.SpectatorList</c> instead of
/// <c>...Localisation.HUD.SpectatorList</c> (the prefix is simply wrong). We therefore never match
/// on file names alone - we use them to work out what a wrong prefix probably meant.
/// </para>
/// </remarks>
internal static class LocalisationMatcher
{
    /// <summary>Minimum share of a class's keys that must exist in a candidate to accept it.</summary>
    private const double keyOverlapThreshold = 0.6;

    public static IReadOnlyDictionary<LocalisationClass, MatchOutcome> Match(
        IReadOnlyList<LocalisationClass> classes,
        IReadOnlyList<ResourceFile> resources)
    {
        var byName = new Dictionary<string, ResourceFile>(StringComparer.Ordinal);
        foreach (ResourceFile resource in resources)
            byName.TryAdd(resource.ResourceName, resource);

        var outcomes = new Dictionary<LocalisationClass, MatchOutcome>();

        foreach (LocalisationClass cls in classes)
        {
            if (byName.TryGetValue(cls.Prefix, out ResourceFile? exact))
            {
                outcomes[cls] = new MatchOutcome
                {
                    Exact = exact,
                    Suggested = null,
                    Ambiguous = false,
                    Candidates = [exact],
                    Suspect = FindSuspect(cls, exact, resources),
                };
                continue;
            }

            List<ResourceFile> candidates = FindCandidates(cls, resources);

            if (candidates.Count == 1)
            {
                outcomes[cls] = suggest(cls, candidates[0], candidates);
                continue;
            }

            if (candidates.Count > 1)
            {
                var scored = candidates
                    .Select(candidate => (Candidate: candidate, Score: KeyOverlap(cls, candidate)))
                    .ToList();

                double best = scored.Max(entry => entry.Score);
                List<ResourceFile> winners = scored
                    .Where(entry => Math.Abs(entry.Score - best) < 1e-9)
                    .Select(entry => entry.Candidate)
                    .ToList();

                if (best >= keyOverlapThreshold && winners.Count == 1)
                {
                    outcomes[cls] = suggest(cls, winners[0], candidates);
                    continue;
                }

                outcomes[cls] = new MatchOutcome
                {
                    Exact = null,
                    Suggested = null,
                    Ambiguous = true,
                    Candidates = candidates,
                };
                continue;
            }

            outcomes[cls] = new MatchOutcome
            {
                Exact = null,
                Suggested = null,
                Ambiguous = false,
                Candidates = [],
            };
        }

        return outcomes;
    }

    private static MatchOutcome suggest(LocalisationClass cls, ResourceFile resource, IReadOnlyList<ResourceFile> candidates)
        => new()
        {
            Exact = null,
            Suggested = resource,
            Ambiguous = false,
            Candidates = candidates,
        };

    /// <summary>
    /// Detects a prefix that resolves to a real resource which nevertheless explains none of the
    /// class's keys, while a neighbouring resource explains all of them.
    /// </summary>
    /// <remarks>
    /// The guards are deliberately tight, because a single-key class whose string is simply missing
    /// from its own resource would otherwise be misreported by any coincidental sibling:
    /// the class must declare at least three distinct keys, share none of them with the matched
    /// resource, and have a sibling that shares every single one.
    /// </remarks>
    private static ResourceFile? FindSuspect(
        LocalisationClass cls,
        ResourceFile matched,
        IReadOnlyList<ResourceFile> resources)
    {
        List<string> keys = cls.Keys.Distinct(StringComparer.Ordinal).ToList();

        if (keys.Count < 3 || keys.Any(matched.HasKey))
            return null;

        foreach (ResourceFile candidate in FindCandidates(cls, resources))
        {
            if (ReferenceEquals(candidate, matched))
                continue;

            if (keys.All(candidate.HasKey))
                return candidate;
        }

        return null;
    }

    private static List<ResourceFile> FindCandidates(LocalisationClass cls, IReadOnlyList<ResourceFile> resources)
    {
        string tail = LastSegment(cls.Prefix);
        string classBase = StripSuffix(cls.ClassName, "Strings");
        string fileName = Path.GetFileNameWithoutExtension(cls.AbsolutePath);
        string fileBase = StripSuffix(fileName, "Strings");

        var matches = resources.Where(resource =>
        {
            string resourceBase = LastSegment(resource.ResourceName);

            return resource.ResourceName.EndsWith($".{tail}", StringComparison.Ordinal)
                   || string.Equals(resourceBase, tail, StringComparison.Ordinal)
                   || string.Equals(resourceBase, classBase, StringComparison.Ordinal)
                   || string.Equals(resourceBase, fileBase, StringComparison.Ordinal)
                   || string.Equals(resourceBase, fileName, StringComparison.Ordinal)
                   || string.Equals(resourceBase + "Strings", fileName, StringComparison.Ordinal);
        }).ToList();

        if (matches.Count <= 1)
            return matches;

        // Directory is a strong hint: HUD classes live beside HUD resources in both repositories.
        List<ResourceFile> sameDirectory = matches
            .Where(candidate => string.Equals(candidate.RelativeDirectory, cls.RelativeDirectory, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return sameDirectory.Count > 0 ? sameDirectory : matches;
    }

    private static double KeyOverlap(LocalisationClass cls, ResourceFile resource)
    {
        List<string> keys = cls.Keys.Distinct(StringComparer.Ordinal).ToList();

        if (keys.Count == 0)
            return 0;

        int hits = keys.Count(key => resource.HasKey(key));
        return (double)hits / keys.Count;
    }

    private static string LastSegment(string value)
    {
        int index = value.LastIndexOf('.');
        return index < 0 ? value : value[(index + 1)..];
    }

    private static string StripSuffix(string value, string suffix)
        => value.EndsWith(suffix, StringComparison.Ordinal) ? value[..^suffix.Length] : value;
}
