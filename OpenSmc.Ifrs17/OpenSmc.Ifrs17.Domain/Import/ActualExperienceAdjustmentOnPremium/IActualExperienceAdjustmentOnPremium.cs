using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Import.WrittenActualCalculation;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.ActualExperienceAdjustmentOnPremium;

public interface IActualExperienceAdjustmentOnPremium : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IActualExperienceAdjustmentOnPremium>(s => s
            .WithApplicability<IDefaultValueIActualExperienceAdjustmentOnPremium>(x => x.Identity.AocType != AocTypes.CF)
            .WithApplicability<IDefaultValueIActualExperienceAdjustmentOnPremium>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA && x.Identity.Novelty != Novelties.C)
            .WithApplicability<IActualExperienceAdjustmentOnPremiumForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA));

    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string? AmountType => AmountTypes.PR;
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => ImportCalculationExtensions.ComputationHelper.ExperienceAdjustEstimateTypeMapping[EstimateTypes.A];

    double Value => GetStorage().GetPremiumAllocationFactor(Identity) *
                    GetStorage().GetPremiums().Sum(pr => GetScope<IWrittenActual>((Identity, pr, EstimateTypes.A, (int?)null)).Value);
}