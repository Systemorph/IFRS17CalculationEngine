namespace OpenSmc.Ifrs17.Domain.DataModel;

public interface IPartitioned
{
    public Guid Partition { get; init; }
}