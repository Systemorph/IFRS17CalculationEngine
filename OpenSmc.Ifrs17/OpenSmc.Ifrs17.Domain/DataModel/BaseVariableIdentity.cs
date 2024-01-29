using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public abstract record BaseVariableIdentity
{
    [NotVisible]
    [Dimension(typeof(GroupOfContract))]
    [IdentityProperty]
    public string DataNode { get; init; }

    [NotVisible]
    [Dimension(typeof(AocType))]
    [IdentityProperty]
    public string AocType { get; init; }

    [NotVisible]
    [Dimension(typeof(Novelty))]
    [IdentityProperty]
    public string Novelty { get; init; }
}