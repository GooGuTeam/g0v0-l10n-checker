// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Model;

/// <summary>
/// An additional resx folder supplied on the command line, optionally paired with the root
/// namespace it is compiled under.
/// </summary>
/// <param name="Path">Absolute path of the folder to scan recursively.</param>
/// <param name="RootNamespace">
/// The project's <c>RootNamespace</c>. Empty means "use the path relative to <paramref name="Path"/>
/// as the whole resource name", which is only correct when the folder is a namespace root itself.
/// </param>
/// <param name="DisplayArgument">The argument exactly as written, used to build readable file paths.</param>
internal sealed record ExtraResxFolder(string Path, string RootNamespace, string DisplayArgument);
