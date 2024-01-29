using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;

public interface INominalDeferrableFromIfrsVariable : INominalDeferrable
{
    double INominalDeferrable.Value => GetStorage().GetValue(Identity.Id, AmountType, EstimateTypes.DA, EconomicBasis, Identity.MonthlyShift, Identity.Id.ProjectionPeriod);
}