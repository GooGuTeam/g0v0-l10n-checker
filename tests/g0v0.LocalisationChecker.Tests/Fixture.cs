// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Tests;

/// <summary>Small helpers so the tests can build models without a real checkout.</summary>
internal static class Fixture
{
    public static readonly string TripleQuote = new('"', 3);

    public static LocalisationMember Member(
        string name,
        string? key,
        string? fallback = null,
        string? documentation = null,
        int line = 1,
        MemberKind kind = MemberKind.Property)
        => new()
        {
            Name = name,
            Kind = kind,
            Line = line,
            Key = key,
            Fallback = fallback,
            Documentation = documentation,
        };

    public static LocalisationClass Class(
        string className,
        string prefix,
        IReadOnlyList<LocalisationMember> members,
        string relativeDirectory = "",
        string? displayPath = null,
        int prefixLine = 1)
        => new()
        {
            AbsolutePath = Path.Combine(Path.GetTempPath(), $"{className}.cs"),
            DisplayPath = displayPath ?? $"{className}.cs",
            ClassName = className,
            Prefix = prefix,
            PrefixLine = prefixLine,
            RelativeDirectory = relativeDirectory,
            Members = members,
        };

    public static ResourceFile Resource(
        string resourceName,
        IReadOnlyList<string> keys,
        string relativeDirectory = "",
        bool inReverseScope = true,
        string? absolutePath = null,
        string? displayPath = null,
        DiagnosticSource source = DiagnosticSource.Resources)
        => new()
        {
            AbsolutePath = absolutePath ?? Path.Combine(Path.GetTempPath(), $"{resourceName}.resx"),
            DisplayPath = displayPath ?? $"{resourceName}.resx",
            Source = source,
            ResourceName = resourceName,
            RelativeDirectory = relativeDirectory,
            InReverseScope = inReverseScope,
            Entries = [.. keys.Select((key, index) => new ResourceEntry(key, key, index + 1))],
        };

    /// <summary>Creates a unique throwaway directory, removed when the returned handle is disposed.</summary>
    public static TempDirectory NewTempDirectory() => new();

    internal sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "g0v0.LocalisationChecker-tests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Write(string relativePath, string content)
        {
            string full = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
            return full;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best effort; the OS will clean the temp directory eventually.
            }
        }
    }
}
