using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.DataTypes.DataModel;

public record ImportIdentity : BaseVariableIdentity
{
    [NotVisible] public bool IsReinsurance { get; init; }

    [NotVisible] public string ValuationApproach { get; init; }

    [NotVisible] public string LiabilityType { get; init; }

    [NotVisible] public int ProjectionPeriod { get; init; }

    public AocStep AocStep => new(AocType, Novelty);

    public ImportScope ImportScope { get; init; }

    public ImportIdentity(RawVariable rv)
    {
        DataNode = rv.DataNode;
        AocType = rv.AocType;
        Novelty = rv.Novelty;
    }

    public ImportIdentity(IfrsVariable iv)
    {
        DataNode = iv.DataNode;
        AocType = iv.AocType;
        Novelty = iv.Novelty;
    }

    public ImportIdentity()
    {
    }
}