namespace OpenSmc.Ifrs17.Domain.Import;

public interface IEmptyINominalCashflow : INominalCashflow
{
    double[] INominalCashflow.Values => Enumerable.Empty<double>().ToArray();
}