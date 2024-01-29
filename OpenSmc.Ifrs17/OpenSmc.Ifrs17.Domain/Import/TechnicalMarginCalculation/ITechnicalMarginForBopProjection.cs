using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForBopProjection : ITechnicalMargin
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<ITechnicalMarginForBopProjection>(s => s
            .WithApplicability<ITechnicalMarginAfterFirstYear>(x => x.Identity.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection));
    double ITechnicalMargin.Value => GetScope<ITechnicalMargin>(Identity with { ProjectionPeriod = 0 }).Value;
}