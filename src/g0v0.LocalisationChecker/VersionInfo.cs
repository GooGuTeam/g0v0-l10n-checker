// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;

namespace g0v0.LocalisationChecker;

/// <summary>
/// The tool's own identity, read from the assembly metadata the SDK writes at build time rather than
/// from a constant in the source.
/// </summary>
/// <remarks>
/// The version therefore has exactly one source — <c>VersionPrefix</c> in
/// <c>Directory.Build.props</c>, or <c>-p:Version=...</c> on the command line — and whatever the tool
/// reports is by construction the version of the binary that reported it. Nothing here is trivia:
/// the same values are echoed into the JSON report so a stored report can be traced back to a build.
/// </remarks>
internal static class VersionInfo
{
    /// <summary>Only used if the assembly somehow carries no <see cref="AssemblyProductAttribute"/>.</summary>
    private const string fallbackProduct = "g0v0.LocalisationChecker";

    private static readonly Assembly assembly = typeof(VersionInfo).Assembly;

    /// <summary>
    /// The raw <see cref="AssemblyInformationalVersionAttribute"/> value: the MSBuild <c>Version</c>
    /// property, plus a <c>+&lt;revision&gt;</c> suffix when the SDK recorded one.
    /// </summary>
    private static readonly string informational = readInformationalVersion();

    private static readonly int metadataSeparator = informational.IndexOf('+');

    /// <summary>The product name, from <c>Product</c> in <c>Directory.Build.props</c>.</summary>
    public static string Product { get; } =
        assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? fallbackProduct;

    /// <summary>The semantic version, with any build metadata (<c>+9f3c1ab</c>) stripped.</summary>
    public static string Version { get; } =
        metadataSeparator < 0 ? informational : informational[..metadataSeparator];

    /// <summary>
    /// The commit the assembly was built from, as recorded by the SDK. Null when the build did not run
    /// inside a git checkout, or when revision stamping was turned off.
    /// </summary>
    public static string? Revision { get; } =
        metadataSeparator >= 0 && metadataSeparator + 1 < informational.Length
            ? informational[(metadataSeparator + 1)..]
            : null;

    /// <summary>The one-line banner printed by <c>--version</c>, always <c>&lt;product&gt; &lt;version&gt;</c>.</summary>
    public static string Display { get; } =
        Revision is null ? $"{Product} {Version}" : $"{Product} {Version}+{Revision}";

    private static string readInformationalVersion()
        => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
           ?? assembly.GetName().Version?.ToString()
           ?? "0.0.0";
}
