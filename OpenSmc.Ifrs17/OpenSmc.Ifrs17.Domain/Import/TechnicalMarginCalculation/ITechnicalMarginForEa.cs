using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.Import.ActualExperienceAdjustmentOnPremium;
using OpenSmc.Ifrs17.Domain.Import.ExperienceAdjustmentForPremium;
using OpenSmc.Ifrs17.Domain.Import.WrittenActualCalculation;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForEa : ITechnicalMargin
{
    static ApplicabilityBuilder ScopeApplicabilityBuilderInner(ApplicabilityBuilder builder) =>
        builder.ForScope<ITechnicalMarginForEa>(s => s
            .WithApplicability<ITechnicalMarginDefaultValue>(x => x.Identity.IsReinsurance)
            .WithApplicability<ITechnicalMarginForEaForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA)
        );
    protected string ReferenceAocType => GetScope<IReferenceAocStep>(Identity).Values.First().AocType;// ReferenceAocStep of EA is CF
    protected double Premiums => GetStorage().GetNovelties(ReferenceAocType, StructureType.AocPresentValue)
                                     .Sum(n => GetScope<IBeExperienceAdjustmentForPremium>(Identity with { AocType = ReferenceAocType, Novelty = n }, o => o.WithContext(EconomicBasis)).Value) -
                                 GetScope<IActualExperienceAdjustmentOnPremium>(Identity with { AocType = ReferenceAocType, Novelty = Novelties.C }).Value;
    protected double Deferrable => GetStorage().GetDeferrableExpenses().Sum(d =>
        GetStorage().GetNovelties(ReferenceAocType, StructureType.AocPresentValue).Sum(n => GetScope<IPvAggregatedOverAccidentYear>((Identity with { AocType = ReferenceAocType, Novelty = n }, d, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value) -
        GetScope<IWrittenActual>((Identity with { AocType = ReferenceAocType, Novelty = Novelties.C }, d, EstimateTypes.A, (int?)null)).Value);
    protected double InvestmentClaims => GetStorage().GetInvestmentClaims().Sum(ic =>
        GetStorage().GetNovelties(ReferenceAocType, StructureType.AocPresentValue).Sum(n => GetScope<IPvAggregatedOverAccidentYear>((Identity with { AocType = ReferenceAocType, Novelty = n }, ic, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value) -
        GetScope<IWrittenActual>((Identity with { AocType = ReferenceAocType, Novelty = Novelties.C }, ic, EstimateTypes.A, (int?)null)).Value);

    double ITechnicalMargin.Value => Premiums + Deferrable + InvestmentClaims;
}