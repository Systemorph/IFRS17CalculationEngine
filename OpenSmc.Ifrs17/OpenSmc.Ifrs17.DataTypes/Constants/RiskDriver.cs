using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.Constants
{
    public record RiskDriver : KeyedOrderedDimension, IHierarchicalDimension
    {
        [Dimension(typeof(RiskDriver))]
        public string Parent { get; init; }
    }
}
