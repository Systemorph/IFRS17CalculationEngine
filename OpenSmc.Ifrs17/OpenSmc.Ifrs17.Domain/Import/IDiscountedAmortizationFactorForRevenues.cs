using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IDiscountedAmortizationFactorForRevenues : IScope<ImportIdentity, ImportStorageOld>
{
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] private string EconomicBasis => GetContext();
    
    double Value => GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.PR, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType == AmountTypes.PR
        ? GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.PR, 0), o => o.WithContext(EconomicBasis)).Value
        : GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).Value;

    string? EffectiveAmountType => GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.PR, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType == AmountTypes.PR
        ? AmountTypes.PR
        : GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType;
}