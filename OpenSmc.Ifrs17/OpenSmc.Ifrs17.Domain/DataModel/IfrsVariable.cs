using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record IfrsVariable : BaseDataRecord
{
    [NotVisible]
    [Dimension(typeof(EconomicBasis))]
    [IdentityProperty]
    public string EconomicBasis { get; init; }
}