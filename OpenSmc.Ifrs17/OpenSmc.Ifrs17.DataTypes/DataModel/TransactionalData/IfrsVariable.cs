using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

public record IfrsVariable : BaseDataRecord
{
    [NotVisible]
    [Dimension(typeof(EconomicBasis))]
    [IdentityProperty]
    public string EconomicBasis { get; init; }
}