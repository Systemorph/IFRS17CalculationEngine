using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableAm : IDiscountedDeferrable
{
    private double AmortizationFactor => GetScope<IDiscountedAmortizationFactorForDeferrals>(Identity, o => o.WithContext(EconomicBasis)).Value;
    private double AggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aocStep => GetScope<IDiscountedDeferrable>(Identity with { AocType = aocStep.AocType, Novelty = aocStep.Novelty }).Value);
    double IDiscountedDeferrable.Value => Math.Abs(AggregatedValue) > Consts.Precision ? -1d * AggregatedValue * AmortizationFactor : default;
}