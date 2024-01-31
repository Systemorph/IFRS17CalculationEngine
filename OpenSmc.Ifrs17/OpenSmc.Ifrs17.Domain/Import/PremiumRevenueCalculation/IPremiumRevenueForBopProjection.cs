using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

public interface IPremiumRevenueForBopProjection : IPremiumRevenue
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IPremiumRevenueForBopProjection>(s => s
            .WithApplicability<IPremiumRevenueAfterFirstYear>(x => x.Identity.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection));
    double IPremiumRevenue.Value => GetScope<IPremiumRevenue>(Identity with { ProjectionPeriod = 0 }).Value;
}