using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IMonthlyAmortizationFactorCashflow : IScope<(ImportIdentity Id, string AmountType, int patternShift), ImportStorageOld>
{
    (string? EffectiveAmountType, double[] Values) releasePattern => GetStorage().GetReleasePattern(Identity.Id, Identity.AmountType, Identity.patternShift);

    private PeriodType PeriodType => GetStorage().GetPeriodType(Identity.AmountType, EstimateTypes.P);
    private double[] MonthlyDiscounting => GetScope<IMonthlyRate>(Identity.Id).Discount;
    private double[] CdcPattern => releasePattern.Values.ComputeDiscountAndCumulate(MonthlyDiscounting, PeriodType); 
    
    [NotVisible] string EconomicBasis => GetContext();
    
    double[] MonthlyAmortizationFactors => Identity.Id.AocType switch {
        AocTypes.AM when releasePattern.Values?.Any() ?? false => releasePattern.Values.Zip(CdcPattern,  //Extract to an other scope with month in the identity to avoid Zip?
            (nominal, discountedCumulated) => Math.Abs(discountedCumulated) >= Consts.Precision ? Math.Max(0, 1 - nominal / discountedCumulated) : 0).ToArray(),
        _ => Enumerable.Empty<double>().ToArray(),
    };
}