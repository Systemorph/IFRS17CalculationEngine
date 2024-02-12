namespace OpenSmc.Ifrs17.Domain.DataModel;

public record AocStep(string AocType, string Novelty)
{
    public Guid Id = Guid.NewGuid();
}