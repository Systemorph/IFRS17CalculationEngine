using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableForBopProjection : IDiscountedDeferrable
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IDiscountedDeferrable>(s => s
            .WithApplicability<IDeferrableAfterFirstYear>(x => x.Identity.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection));
    double IDiscountedDeferrable.Value => GetScope<IDiscountedDeferrable>(Identity with { ProjectionPeriod = 0 }).Value;
}