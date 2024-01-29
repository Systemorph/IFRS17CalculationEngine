namespace OpenSmc.Ifrs17.Domain.Import;

public interface IDiscountedCashflowNextYearsProjection : IDiscountedCashflow{
    double[] IDiscountedCashflow.Values => GetScope<IDiscountedCashflow>((Identity.Id with {ProjectionPeriod = GetStorage().FirstNextYearProjection}, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear)).Values;
}