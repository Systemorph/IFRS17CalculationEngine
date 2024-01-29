using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IDiscountedAmortizationFactorForDeferrals : IScope<ImportIdentity, ImportStorage>
{
    private string EconomicBasis => GetContext();
    double Value => GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.DAE, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType == AmountTypes.DAE
        ? GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.DAE, 0), o => o.WithContext(EconomicBasis)).Value
        : GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).Value;
    string? EffectiveAmountType => GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.DAE, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType == AmountTypes.DAE
        ? AmountTypes.DAE
        : GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType;
}