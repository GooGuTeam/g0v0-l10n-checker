// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// One <c>public static LocalisableString ...</c> member of a localisation class.
/// </summary>
internal sealed class LocalisationMember
{
    public required string Name { get; init; }

    public required MemberKind Kind { get; init; }

    /// <summary>1-based line of the member name.</summary>
    public required int Line { get; init; }

    /// <summary>The literal passed to <c>getKey(@"...")</c>, or null when it could not be read.</summary>
    public required string? Key { get; init; }

    /// <summary>The English fallback literal, with verbatim escaping already resolved.</summary>
    public required string? Fallback { get; init; }

    /// <summary>The translation text carried by the member's XML doc, HTML entities already decoded.</summary>
    public required string? Documentation { get; init; }
}
