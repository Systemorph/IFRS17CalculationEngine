using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface IPremiumRevenue : IScope<ImportIdentity, ImportStorageOld>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IPremiumRevenue>(s => s
            .WithApplicability<IPremiumRevenueWithIfrsVariable>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow || x.GetStorage().IsSecondaryScope(x.Identity.DataNode))
            .WithApplicability<IPremiumRevenueForBopProjection>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && x.Identity.ProjectionPeriod > 0)
            .WithApplicability<IPremiumRevenueForBop>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I)
            .WithApplicability<IPremiumRevenueForIaStandard>(x => x.Identity.AocType == AocTypes.IA) // && x.Identity.Novelty == Novelties.I)
            //.WithApplicability<PremiumRevenueForIaNewBusiness>(x => x.Identity.AocType == AocTypes.IA)
            .WithApplicability<IPremiumRevenueDefaultValue>(x => new[] { AocTypes.CF, AocTypes.EA }.Contains(x.Identity.AocType))
            //TODO compute EA but in the case of no LC EA is 0
            .WithApplicability<IPremiumRevenueAm>(x => x.Identity.AocType == AocTypes.AM)
            .WithApplicability<IPremiumRevenueEop>(x => x.Identity.AocType == AocTypes.EOP)
        );
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => EstimateTypes.R;
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string? AmountType => null;
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);

    double Value => GetStorage().GetPremiums().Sum(at =>
        GetScope<IPresentValue>((Identity, at, EstimateTypes.BE, (int?)null), o => o.WithContext(EconomicBasis)).Value);
}