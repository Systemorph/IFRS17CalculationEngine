using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

public interface IPresentValueWithInterestAccretion : IPresentValue, IWithInterestAccretion
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IPresentValueWithInterestAccretion>(s => s.WithApplicability<IPresentValueWithInterestAccretionForCreditRisk>(x => x.Identity.Id.IsReinsurance && x.GetStorage().GetCdr().Contains(x.Identity.AmountType)));
    [NotVisible] double[] IPresentValue.Values => GetInterestAccretion();
}