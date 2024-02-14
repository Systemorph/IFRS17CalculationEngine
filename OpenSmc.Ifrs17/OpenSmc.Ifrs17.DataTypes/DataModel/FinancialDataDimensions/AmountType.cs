using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;

public record AmountType : KeyedOrderedDimensionWithExternalId, IHierarchicalDimension
{
    public AmountType(string systemName, string displayName, string parent, int order, PeriodType periodType)
    {
        SystemName = systemName;
        DisplayName = displayName;
        Parent = parent;
        Order = order;
        PeriodType = periodType;
    }
        
    [Dimension(typeof(AmountType))] public string? Parent { get; init; }

    [Dimension(typeof(PeriodType))] public PeriodType PeriodType { get; init; }

}