// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N005 - the XML doc text above a member must match the member's English fallback.
/// </summary>
/// <remarks>
/// Both are produced from the same source of truth by the localisation analyser, so a disagreement
/// means one of them was edited by hand. The comparison is done after HTML-decoding the doc
/// (<c>&amp;#39;</c> is <c>'</c>) and normalising line endings (the doc uses LF, the verbatim
/// literal inherits CRLF from the file).
/// </remarks>
internal sealed class DocFallbackMismatchChecker : IChecker
{
    public string RuleId => RuleIds.DocFallbackMismatch;

    public Severity DefaultSeverity => Severity.Error;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        foreach (LocalisationClass cls in context.Workspace.Classes)
        {
            foreach (LocalisationMember member in cls.Members)
            {
                if (member.Documentation is null || member.Fallback is null)
                    continue;

                string documentation = CheckerText.NormaliseNewlines(member.Documentation);
                string fallback = CheckerText.NormaliseNewlines(member.Fallback);

                if (string.Equals(documentation, fallback, StringComparison.Ordinal))
                    continue;

                yield return new Diagnostic(
                    RuleId,
                    severity,
                    DiagnosticSource.Osu,
                    cls.DisplayPath,
                    member.Line,
                    $"documentation and fallback text differ (doc: \"{CheckerText.Snippet(documentation)}\"; "
                    + $"fallback: \"{CheckerText.Snippet(fallback)}\")",
                    Key: member.Key);
            }
        }
    }
}
