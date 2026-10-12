// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Output;

/// <summary>ANSI colouring, tuned for dark terminals.</summary>
internal sealed class Ansi
{
    private readonly bool enabled;

    private Ansi(bool enabled) => this.enabled = enabled;

    public static Ansi Plain { get; } = new(false);

    public static Ansi Coloured { get; } = new(true);

    public string ForSeverity(Severity severity, string text) => severity switch
    {
        Severity.Error => Wrap("91", text),
        Severity.Warning => Wrap("93", text),
        _ => Wrap("96", text),
    };

    public string Dim(string text) => Wrap("2", text);

    public string Path(string text) => Wrap("36", text);

    private string Wrap(string code, string text)
        => enabled ? $"\u001b[{code}m{text}\u001b[0m" : text;
}
