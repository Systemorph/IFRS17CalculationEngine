using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Arithmetics.Api;
using Systemorph.Vertex.Scopes.Api;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record AocConfiguration : KeyedRecord, IWithYearAndMonth, IOrdered
{
    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; }

    [IdentityProperty]
    [Dimension(typeof(AocType))]
    public string AocType { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Novelty))]
    public string Novelty { get; init; }

    [Dimension(typeof(Constants.DataType))] public Constants.DataType DataType { get; init; }

    [Dimension(typeof(StructureType))] public StructureType StructureType { get; init; }

    [Dimension(typeof(InputSource))] public InputSource InputSource { get; init; }

    [Dimension(typeof(FxPeriod))] public FxPeriod FxPeriod { get; init; }

    [Dimension(typeof(PeriodType), nameof(YcPeriod))]
    public PeriodType YcPeriod { get; init; }

    [Dimension(typeof(PeriodType), nameof(CdrPeriod))]
    public PeriodType CdrPeriod { get; init; }

    [Dimension(typeof(ValuationPeriod))] public ValuationPeriod ValuationPeriod { get; init; }

    [Dimension(typeof(PeriodType), nameof(RcPeriod))]
    public PeriodType RcPeriod { get; init; }

    [NotVisible] public int Order { get; init; }
}