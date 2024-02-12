using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Domain.Abstractions.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;

public record IfrsVariable : BaseDataRecord
{
    [NotVisible]
    [Dimension(typeof(EconomicBasis))]
    [IdentityProperty]
    public string EconomicBasis { get; init; }
}