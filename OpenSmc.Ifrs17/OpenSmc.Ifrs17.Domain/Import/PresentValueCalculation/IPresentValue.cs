using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

public interface IPresentValue : IWithGetValueFromValues
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IPresentValue>(s => s
            .WithApplicability<IComputeIPresentValueWithIfrsVariable>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow || x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode))
            .WithApplicability<IPresentValueFromDiscountedCashflow>(x => x.Identity.Id.AocType == AocTypes.BOP && x.Identity.Id.Novelty != Novelties.C || x.Identity.Id.AocType == AocTypes.EOP)
            .WithApplicability<ICashflowAocStep>(x => x.Identity.Id.AocType == AocTypes.CF)
            .WithApplicability<IPresentValueWithInterestAccretion>(x => x.Identity.Id.AocType == AocTypes.IA)
            .WithApplicability<IEmptyValuesAocStep>(x => !x.GetStorage().GetAllAocSteps(StructureType.AocPresentValue).Contains(x.Identity.Id.AocStep) ||
                                                        x.Identity.Id.AocType == AocTypes.CRU && !x.GetStorage().GetCdr().Contains(x.Identity.AmountType))
        );

    [NotVisible]
    [IdentityProperty]
    [Dimension(typeof(EconomicBasis))]
    string EconomicBasis => GetContext();

    [NotVisible]
    double[] Values => GetScope<ITelescopicDifference>(Identity).Values;

    public double Value => GetValueFromValues(Values);
}