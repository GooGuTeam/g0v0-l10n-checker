// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Output;

internal enum ColorMode
{
    Auto,
    Always,
    Never,
}

/// <summary>
/// Decides whether ANSI colour should be emitted. The order matters: an explicit <c>FORCE_COLOR</c>
/// wins over everything, then <c>NO_COLOR</c>, then CI detection (colouring a GitHub annotation
/// would corrupt it), and only then the terminal check.
/// </summary>
internal static class ColorModeResolver
{
    public static bool UseColor(ColorMode mode)
    {
        if (mode == ColorMode.Always)
            return true;

        if (mode == ColorMode.Never)
            return false;

        string? forceColor = Environment.GetEnvironmentVariable("FORCE_COLOR");
        if (!string.IsNullOrEmpty(forceColor))
            return !(forceColor == "0" || forceColor.Equals("false", StringComparison.OrdinalIgnoreCase));

        string? noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        if (!string.IsNullOrEmpty(noColor))
            return false;

        if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            return false;

        try
        {
            return !Console.IsOutputRedirected;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
