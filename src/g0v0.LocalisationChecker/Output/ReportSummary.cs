// Copyright (c) GooGuTeam <https://github.com/GooGuTeam>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace g0v0.LocalisationChecker.Output;

/// <summary>Totals printed (or serialised) at the end of a run.</summary>
internal sealed class ReportSummary
{
    public required int Classes { get; init; }

    public required int Resources { get; init; }

    public required int SkippedStringsFiles { get; init; }

    public required int Errors { get; init; }

    public required int Warnings { get; init; }

    public required int Infos { get; init; }

    /// <summary>Diagnostics hidden because they are listed in the baseline.</summary>
    public required int Suppressed { get; init; }

    public int Total => Errors + Warnings + Infos;
}
