namespace OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;

public interface IPartitioned
{
    public Guid Partition { get; init; }
}