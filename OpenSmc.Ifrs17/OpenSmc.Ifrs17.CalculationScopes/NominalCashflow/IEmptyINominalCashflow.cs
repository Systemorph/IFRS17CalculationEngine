namespace OpenSms.Ifrs17.CalculationScopes.NominalCashflow;

public interface IEmptyINominalCashflow : INominalCashflow
{
    double[] INominalCashflow.Values => Enumerable.Empty<double>().ToArray();
}