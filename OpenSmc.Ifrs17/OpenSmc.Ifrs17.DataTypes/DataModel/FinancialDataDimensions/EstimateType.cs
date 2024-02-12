using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;

public record EstimateType : KeyedOrderedDimensionWithExternalId
{
    public InputSource InputSource { get; init; }

    public StructureType StructureType { get; init; }

    [Dimension(typeof(PeriodType))] public PeriodType PeriodType { get; init; }
}