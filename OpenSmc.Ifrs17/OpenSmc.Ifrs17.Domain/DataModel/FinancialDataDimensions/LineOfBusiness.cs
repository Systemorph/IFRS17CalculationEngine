using OpenSmc.Domain.Abstractions;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;

public record LineOfBusiness : KeyedOrderedDimension, IHierarchicalDimension, Systemorph.Vertex.Api.IHierarchicalDimension
{
    [Dimension(typeof(LineOfBusiness))] public string Parent { get; init; }
}