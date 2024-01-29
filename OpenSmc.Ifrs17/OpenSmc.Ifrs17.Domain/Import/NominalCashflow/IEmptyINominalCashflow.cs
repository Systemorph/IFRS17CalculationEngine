namespace OpenSmc.Ifrs17.Domain.Import.NominalCashflow;

public interface IEmptyINominalCashflow : INominalCashflow
{
    double[] INominalCashflow.Values => Enumerable.Empty<double>().ToArray();
}