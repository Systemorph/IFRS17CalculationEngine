using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record GroupOfContract : DataNode
{
    [NotVisible]
    [Dimension(typeof(int), nameof(AnnualCohort))]
    //[Immutable]
    public int AnnualCohort { get; init; }

    [NotVisible]
    [Dimension(typeof(LiabilityType))]
    //[Immutable]
    public string LiabilityType { get; init; }

    [NotVisible]
    [Dimension(typeof(Profitability))]
    //[Immutable]
    public string Profitability { get; init; }

    [Required]
    [NotVisible]
    [Dimension(typeof(Portfolio))]
    //[Immutable]
    public string Portfolio { get; init; }

    [NotVisible]
    //[Immutable]
    public string YieldCurveName { get; init; }

    public virtual string Partner { get; init; }
}