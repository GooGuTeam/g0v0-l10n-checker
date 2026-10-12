// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using g0v0.LocalisationChecker.Model;

namespace g0v0.LocalisationChecker.Output;

internal interface IReporter
{
    void Report(IReadOnlyList<Diagnostic> diagnostics, ReportSummary summary, TextWriter writer);
}
