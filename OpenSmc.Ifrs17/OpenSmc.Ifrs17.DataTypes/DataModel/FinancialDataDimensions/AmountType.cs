using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;

public record AmountType : KeyedOrderedDimensionWithExternalId, IHierarchicalDimension
{
    public Guid Id { get; set; }

    [Dimension(typeof(AmountType))] public string Parent { get; init; }

    [Dimension(typeof(PeriodType))] public PeriodType PeriodType { get; init; }

    public AmountType()
    {
        Id = Guid.NewGuid();
    }
}