// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Checking.Checkers;

/// <summary>
/// L10N004 - the same key declared twice inside one class. One of the two members can never be
/// translated independently of the other. The localisation analyser reports this as LOC004, but
/// only while editing; nothing catches it in CI.
/// </summary>
internal sealed class DuplicateKeyChecker : IChecker
{
    public string RuleId => RuleIds.DuplicateKeyInClass;

    public Severity DefaultSeverity => Severity.Error;

    public IEnumerable<Diagnostic> Run(CheckContext context)
    {
        Severity severity = context.Options.SeverityFor(RuleId, DefaultSeverity);

        foreach (LocalisationClass cls in context.Workspace.Classes)
        {
            var firstLineByKey = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (LocalisationMember member in cls.Members)
            {
                if (member.Key is null)
                    continue;

                if (firstLineByKey.TryGetValue(member.Key, out int firstLine))
                {
                    yield return new Diagnostic(
                        RuleId,
                        severity,
                        DiagnosticSource.Osu,
                        cls.DisplayPath,
                        member.Line,
                        $"duplicate key '{member.Key}' (already declared at line {firstLine})",
                        Key: member.Key);
                    continue;
                }

                firstLineByKey[member.Key] = member.Line;
            }
        }
    }
}
