using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

namespace OpenSmc.Ifrs17.Domain.Import.LossRecoveryComponent;

public interface ILossRecoveryComponentForAm : ILossRecoveryComponent
{
    private string economicBasis => GetScope<ITechnicalMargin>(Identity).EconomicBasis;
    double ILossRecoveryComponent.Value => Math.Abs(AggregatedValue) > Consts.Precision ? -1d * AggregatedValue * GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(economicBasis)).Value : default;
}