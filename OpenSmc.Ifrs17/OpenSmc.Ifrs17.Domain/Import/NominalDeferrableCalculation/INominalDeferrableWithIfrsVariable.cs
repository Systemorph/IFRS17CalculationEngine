namespace OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;

public interface INominalDeferrableWithIfrsVariable : INominalDeferrable
{
    double INominalDeferrable.Value => GetStorage().GetValue(Identity.Id, AmountType, EstimateType, EconomicBasis, Identity.MonthlyShift, Identity.Id.ProjectionPeriod);
}