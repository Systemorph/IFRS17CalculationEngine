namespace OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

public interface IComputeIPresentValueWithIfrsVariable : IPresentValue
{
    double IPresentValue.Value => GetStorage().GetValue(Identity.Id, Identity.AmountType, Identity.EstimateType, EconomicBasis, Identity.AccidentYear, Identity.Id.ProjectionPeriod);
    double[] IPresentValue.Values => Enumerable.Empty<double>().ToArray();
}