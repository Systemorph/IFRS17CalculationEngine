namespace OpenSmc.Ifrs17.Domain.Import.AccrualActual;

public interface IAccrualEmptyValues : IAccrualActual
{
    double IAccrualActual.Value => default;
}