using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Attributes.Arithmetics;

namespace OpenSmc.Ifrs17.Domain.Report;

public record RawVariableReport
{
    [NotVisible]
    [Dimension(typeof(GroupOfContract))]
    public string? DataNode { get; init; }

    [NotVisible]
    [AggregateBy]
    [Dimension(typeof(AocType))]
    public string? AocType { get; init; }

    [NotVisible]
    [Dimension(typeof(Novelty))]
    public string? Novelty { get; init; }

    [NotVisible]
    [AggregateBy]
    [Dimension(typeof(AmountType))]
    public string? AmountType { get; init; }

    [NotVisible]
    [Dimension(typeof(EstimateType))]
    public string? EstimateType { get; init; }

    [NotVisible]
    [Dimension(typeof(int), nameof(Index))]
    public int Index { get; init; }

    public double Value { get; init; }
}