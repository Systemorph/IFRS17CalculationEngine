namespace OpenSmc.Ifrs17.Domain.Import.WrittenActualCalculation;

public interface IActualEmptyValue : IWrittenActual
{
    double IWrittenActual.Value => default;
}