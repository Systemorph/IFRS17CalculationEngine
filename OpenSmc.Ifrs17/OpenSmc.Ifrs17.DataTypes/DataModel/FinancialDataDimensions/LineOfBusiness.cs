using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;

public record LineOfBusiness : KeyedOrderedDimension, IHierarchicalDimension
{
    [Dimension(typeof(LineOfBusiness))] public string Parent { get; init; }
}