// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// The shape a localisation member was declared in.
/// </summary>
internal enum MemberKind
{
    /// <summary>A parameterless read-only property.</summary>
    Property,

    /// <summary>A static method taking format arguments.</summary>
    Method,

    /// <summary>A member wrapped in <c>PluralisableString</c>.</summary>
    Plural,
}
