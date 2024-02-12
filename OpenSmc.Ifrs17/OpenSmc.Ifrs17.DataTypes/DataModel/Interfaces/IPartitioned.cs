namespace OpenSmc.Ifrs17.Domain.DataModel.Interfaces;

public interface IPartitioned
{
    public Guid Partition { get; init; }
}