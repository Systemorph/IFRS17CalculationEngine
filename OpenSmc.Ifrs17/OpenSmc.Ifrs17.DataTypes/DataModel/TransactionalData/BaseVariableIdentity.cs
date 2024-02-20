using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

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