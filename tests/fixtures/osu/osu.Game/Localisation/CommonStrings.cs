// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class CommonStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.Common";

        /// <summary>
        /// "Back"
        /// </summary>
        public static LocalisableString Back => new TranslatableString(getKey(@"back"), @"Back");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
