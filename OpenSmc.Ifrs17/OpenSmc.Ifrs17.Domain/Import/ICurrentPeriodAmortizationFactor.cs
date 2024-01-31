using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ICurrentPeriodAmortizationFactor : IScope<(ImportIdentity Id, string AmountType, int patternShift), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<ICurrentPeriodAmortizationFactor>(s => 
            s.WithApplicability<IAmfFromIfrsVariable>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow
                                                          || x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode)));

    private int Shift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    private int TimeStep => GetStorage().GetTimeStep(Identity.Id.ProjectionPeriod);
    private double AmortizedFactor => GetScope<IMonthlyAmortizationFactorCashflow>(Identity)
        .MonthlyAmortizationFactors
        .Skip(Shift)
        .Take(TimeStep)
        .DefaultIfEmpty()
        .Aggregate(1d, (x, y) => x * y);
                            
    [NotVisible] string EconomicBasis => GetContext();

    string EstimateType => EstimateTypes.F;
    string? EffectiveAmountType => GetScope<IMonthlyAmortizationFactorCashflow>(Identity).releasePattern.EffectiveAmountType;
    double Value => 1d - AmortizedFactor;
}