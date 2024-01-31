using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForAm : ITechnicalMargin
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<ITechnicalMarginForAm>(s => s.WithApplicability<ITechnicalMarginForAmForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA));

    double ITechnicalMargin.Value => Math.Abs(AggregatedValue) > Consts.Precision ? -1d * AggregatedValue * GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).Value : default;
}