using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.ExperienceAdjustmentForPremium;

public interface IBeExperienceAdjustmentForPremium : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IBeExperienceAdjustmentForPremium>(s => s
            .WithApplicability<IDefaultValueIBeExperienceAdjustmentForPremium>(x => x.Identity.AocType != AocTypes.CF)
            .WithApplicability<IDefaultValueIBeExperienceAdjustmentForPremium>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA && x.Identity.Novelty != Novelties.C)
            .WithApplicability<IBeExperienceAdjustmentForPremiumForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA)
        );

    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => ImportCalculationExtensions.ComputationHelper.ExperienceAdjustEstimateTypeMapping[EstimateTypes.BE];
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] string EconomicBasis => GetContext();
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string? AmountType => AmountTypes.PR;

    double Value => GetStorage().GetPremiumAllocationFactor(Identity) *
                    GetStorage().GetPremiums().Sum(pr => GetScope<IPvAggregatedOverAccidentYear>((Identity, pr, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value);
}