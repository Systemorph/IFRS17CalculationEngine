using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;

public interface IAmDeferrable : INominalDeferrable
{
    private IEnumerable<AocStep> ReferenceAocSteps => GetScope<IReferenceAocStep>(Identity.Id).Values; //Reference step of AM,C is CL,C
    private double ReferenceCashflow => ReferenceAocSteps.Sum(refAocStep => GetScope<IAmReferenceDeferrable>((Identity.Id with { AocType = refAocStep.AocType, Novelty = refAocStep.Novelty }, Identity.MonthlyShift)).Value);

    double INominalDeferrable.Value => Math.Abs(ReferenceCashflow) > Consts.Precision ? -1d * ReferenceCashflow * GetScope<ICurrentPeriodAmortizationFactor>((Identity.Id, AmountTypes.DAE, Identity.MonthlyShift), o => o.WithContext(EconomicBasis)).Value : default;
}