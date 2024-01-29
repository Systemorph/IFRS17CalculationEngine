using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IAmfFromIfrsVariable : ICurrentPeriodAmortizationFactor{
    private double AmortizationFactorForAmountType => GetStorage().GetValue(Identity.Id, Identity.AmountType, EstimateType, EconomicBasis, 
        Identity.patternShift == 0 ? null : Identity.patternShift, Identity.Id.ProjectionPeriod); //TODO shift of 0 is a valid value
    
    private double AmortizationFactorFromPattern => GetStorage().GetValue(Identity.Id, null, EstimateType, EconomicBasis, Identity.patternShift == 0 ? null : Identity.patternShift, Identity.Id.ProjectionPeriod);
    
    private double AmortizationFactorForCu => GetStorage().GetValue(Identity.Id, AmountTypes.CU, EstimateType, EconomicBasis, 
        Identity.patternShift == 0 ? null : Identity.patternShift, Identity.Id.ProjectionPeriod);

    double ICurrentPeriodAmortizationFactor.Value => Math.Abs(AmortizationFactorForAmountType) >= Consts.Precision ? AmortizationFactorForAmountType 
        : Math.Abs(AmortizationFactorFromPattern) >= Consts.Precision ? AmortizationFactorFromPattern : AmortizationFactorForCu;
    string? ICurrentPeriodAmortizationFactor.EffectiveAmountType => Math.Abs(AmortizationFactorForAmountType) >= Consts.Precision ? Identity.AmountType 
        : Math.Abs(AmortizationFactorFromPattern) >= Consts.Precision ? null : AmountTypes.CU;
}