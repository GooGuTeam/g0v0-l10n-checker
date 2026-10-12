// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class BrokenStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.Broken";

        /// <summary>
        /// "Present"
        /// </summary>
        public static LocalisableString Present => new TranslatableString(getKey(@"present"), @"Present");

        /// <summary>
        /// "Missing"
        /// </summary>
        public static LocalisableString Missing => new TranslatableString(getKey(@"missing"), @"Missing");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
