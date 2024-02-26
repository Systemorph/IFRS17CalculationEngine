using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;

public record EstimateType : KeyedOrderedDimensionWithExternalId
{
    public InputSource InputSource { get; init; }

    public string StructureType { get; init; }

    [Dimension(typeof(PeriodType))] public PeriodType PeriodType { get; init; }
}