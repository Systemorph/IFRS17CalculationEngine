using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;

public interface INominalDeferrable : IScope<(ImportIdentity Id, int MonthlyShift), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<INominalDeferrable>(s => s
            .WithApplicability<INominalDeferrableWithIfrsVariable>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow || x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode))
            .WithApplicability<BoPDeferrableProjection>(x => x.Identity.Id.AocType == AocTypes.BOP && x.Identity.Id.Novelty == Novelties.I && x.Identity.Id.ProjectionPeriod > 0)
            .WithApplicability<BoPDeferrable>(x => x.Identity.Id.AocType == AocTypes.BOP)
            .WithApplicability<AmDeferrable>(x => x.Identity.Id.AocType == AocTypes.AM)
            .WithApplicability<EopDeferrable>(x => x.Identity.Id.AocType == AocTypes.EOP)
        );

    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => EstimateTypes.DA;
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string AmountType => null;
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] string EconomicBasis => EconomicBases.N;
    double Value => default;
}