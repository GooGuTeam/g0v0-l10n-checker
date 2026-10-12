// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;
using g0v0.LocalisationChecker.Parsing;

namespace g0v0.LocalisationChecker.Workspace;

/// <summary>
/// Scans the osu! and osu-resources checkouts and turns them into parsed models.
/// </summary>
internal static class WorkspaceLoader
{
    private const string localisationFolderName = "Localisation";
    private const string resourcesProjectFolder = "osu.Game.Resources";
    private const string resourcesNamespace = "osu.Game.Resources.Localisation";

    /// <summary>
    /// Sub-directories of <c>Localisation/</c> whose base resx files take part in the reverse check.
    /// <c>Web/**</c> is intentionally absent - it is generated for the website, not consumed by the
    /// hand-written <c>*Strings.cs</c> classes.
    /// </summary>
    private static readonly HashSet<string> reverseScopeDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Catch", "HUD", "Mania", "Osu", "SkinComponents", "Taiko",
        };

    private static readonly string[] ignoredDirectoryNames =
        ["bin", "obj", ".git", ".vs", "node_modules", "packages"];

    public static LoadedWorkspace Load(
        string osuRoot,
        string resourcesRoot,
        IReadOnlyList<ExtraResxFolder> extraFolders)
    {
        osuRoot = Path.GetFullPath(osuRoot);
        resourcesRoot = Path.GetFullPath(resourcesRoot);

        if (!Directory.Exists(osuRoot))
            throw new WorkspaceException($"osu repository root does not exist: {osuRoot}");

        if (!Directory.Exists(resourcesRoot))
            throw new WorkspaceException($"osu-resources repository root does not exist: {resourcesRoot}");

        var classes = new List<LocalisationClass>();
        int skipped = 0;

        foreach (string file in EnumerateFiles(osuRoot, "*Strings.cs"))
        {
            string source;
            try
            {
                source = File.ReadAllText(file);
            }
            catch (IOException ex)
            {
                throw new WorkspaceException($"failed to read '{file}': {ex.Message}");
            }

            string localisationRoot = FindLocalisationRoot(file);
            string relativeDirectory = ResourceNameResolver.RelativeDirectory(file, localisationRoot);

            LocalisationClass? parsed = StringsFileParser.Parse(
                file,
                ToDisplayPath(osuRoot, file),
                relativeDirectory,
                source);

            if (parsed is null)
            {
                skipped++;
                continue;
            }

            classes.Add(parsed);
        }

        var resources = new List<ResourceFile>();
        resources.AddRange(LoadResourcesRepository(resourcesRoot));

        foreach (ExtraResxFolder folder in extraFolders)
            resources.AddRange(LoadExtraFolder(folder));

        return new LoadedWorkspace
        {
            OsuRoot = osuRoot,
            ResourcesRoot = resourcesRoot,
            Classes = classes,
            Resources = resources,
            SkippedStringsFiles = skipped,
        };
    }

    private static IEnumerable<ResourceFile> LoadResourcesRepository(string resourcesRoot)
    {
        string localisationDir = Path.Combine(resourcesRoot, resourcesProjectFolder, localisationFolderName);

        if (!Directory.Exists(localisationDir))
        {
            throw new WorkspaceException(
                $"could not find '{resourcesProjectFolder}/{localisationFolderName}' under {resourcesRoot}. " +
                "Pass the osu-resources repository root with --resources.");
        }

        foreach (string file in EnumerateFiles(localisationDir, "*.resx"))
        {
            if (!ResxParser.IsBaseResource(file))
                continue;

            string relativeDirectory = ResourceNameResolver.RelativeDirectory(file, localisationDir);
            bool inReverseScope = relativeDirectory.Length == 0 || reverseScopeDirectories.Contains(relativeDirectory);

            yield return new ResourceFile
            {
                AbsolutePath = file,
                DisplayPath = ToDisplayPath(resourcesRoot, file),
                Source = DiagnosticSource.Resources,
                ResourceName = ResourceNameResolver.Resolve(file, localisationDir, resourcesNamespace),
                RelativeDirectory = relativeDirectory,
                InReverseScope = inReverseScope,
                Entries = ResxParser.Parse(file),
            };
        }
    }

    private static IEnumerable<ResourceFile> LoadExtraFolder(ExtraResxFolder folder)
    {
        if (!Directory.Exists(folder.Path))
            throw new WorkspaceException($"extra resx folder does not exist: {folder.Path}");

        string prefix = folder.DisplayArgument.TrimEnd('/', '\\');

        foreach (string file in EnumerateFiles(folder.Path, "*.resx"))
        {
            if (!ResxParser.IsBaseResource(file))
                continue;

            string relative = Path.GetRelativePath(folder.Path, file).Replace('\\', '/');

            yield return new ResourceFile
            {
                AbsolutePath = file,
                DisplayPath = $"{prefix}/{relative}",
                Source = DiagnosticSource.Extra,
                ResourceName = ResourceNameResolver.Resolve(file, folder.Path, folder.RootNamespace),
                RelativeDirectory = ResourceNameResolver.RelativeDirectory(file, folder.Path),
                // Extra folders only take part in the forward check; we do not know whether they are
                // expected to mirror the game's own conventions, so we do not report orphans for them.
                InReverseScope = false,
                Entries = ResxParser.Parse(file),
            };
        }
    }

    /// <summary>
    /// Walks up from a class file until it finds the directory named <c>Localisation</c>, so that
    /// <c>HUD/SpectatorListStrings.cs</c> and <c>Localisation/CommonStrings.cs</c> yield <c>HUD</c>
    /// and <c>""</c> respectively - directly comparable with a resx file's relative directory.
    /// </summary>
    private static string FindLocalisationRoot(string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);

        while (!string.IsNullOrEmpty(directory))
        {
            if (string.Equals(Path.GetFileName(directory), localisationFolderName, StringComparison.Ordinal))
                return directory;

            directory = Path.GetDirectoryName(directory);
        }

        return Path.GetDirectoryName(filePath) ?? string.Empty;
    }

    private static string ToDisplayPath(string root, string file)
        => Path.GetRelativePath(root, file).Replace('\\', '/');

    /// <summary>Recursive file enumeration that never descends into build output or VCS folders.</summary>
    private static IEnumerable<string> EnumerateFiles(string root, string pattern)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            string directory = pending.Pop();

            IEnumerable<string> subdirectories;
            try
            {
                subdirectories = Directory.EnumerateDirectories(directory);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }

            foreach (string subdirectory in subdirectories)
            {
                if (ignoredDirectoryNames.Contains(Path.GetFileName(subdirectory), StringComparer.OrdinalIgnoreCase))
                    continue;

                pending.Push(subdirectory);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory, pattern);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }

            foreach (string file in files)
                yield return file;
        }
    }
}
