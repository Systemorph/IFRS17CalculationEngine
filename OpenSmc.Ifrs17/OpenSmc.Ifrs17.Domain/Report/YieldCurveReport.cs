using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Domain.Report;

public record YieldCurveReport : KeyedRecord, IWithYearMonthAndScenario
{
    [NotVisible]
    [Dimension(typeof(Currency))]
    public string? Currency { get; init; }

    [NotVisible]
    [Dimension(typeof(int), nameof(Year))]
    public int Year { get; init; }

    [NotVisible]
    [Dimension(typeof(int), nameof(Month))]
    public int Month { get; init; }

    [NotVisible]
    [Dimension(typeof(Scenario))]
    public string? Scenario { get; init; }

    [NotVisible]
    [Dimension(typeof(int), nameof(Index))]
    public int Index { get; init; }

    public double Value { get; init; }
}