using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDiscountedDeferrable : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IDiscountedDeferrable>(s => s
            .WithApplicability<IDeferrableWithIfrsVariable>(x => x.GetStorage().IsSecondaryScope(x.Identity.DataNode))
            .WithApplicability<IDeferrableForBopProjection>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && x.Identity.ProjectionPeriod > 0)
            .WithApplicability<IDeferrableForBop>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I)
            .WithApplicability<IDeferrableForIaStandard>(x => x.Identity.AocType == AocTypes.IA) // && x.Identity.Novelty == Novelties.I)
                                                                                                //WithApplicability<DeferrableForIaNewBusiness>(x => x.Identity.AocType == AocTypes.IA)
            .WithApplicability<IDeferrableDefaultValue>(x => x.Identity.AocType == AocTypes.CF)
            .WithApplicability<IDeferrableEa>(x => x.Identity.AocType == AocTypes.EA)
            .WithApplicability<IDeferrableAm>(x => x.Identity.AocType == AocTypes.AM)
            .WithApplicability<IDeferrableEop>(x => x.Identity.AocType == AocTypes.EOP)
        );
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => EstimateTypes.DA;
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string AmountType => null;
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);

    double Value => GetStorage().GetDeferrableExpenses().Sum(at =>
        GetScope<IPresentValue>((Identity, at, EstimateTypes.BE, (int?)null), o => o.WithContext(EconomicBasis)).Value);
}