using OpenSmc.Collections;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Scopes;

namespace OpenSms.Ifrs17.CalculationScopes.AocSteps;

public interface IReferenceAocStep : IScope<ImportIdentity, ImportStorage>
{
    static int FirstNextYearProjection() => 0;

    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IReferenceAocStep>(s => s.WithApplicability<IReferenceAocStepForProjections>(x => 
            x.Identity.ProjectionPeriod >= FirstNextYearProjection()));

    protected IEnumerable<AocStep> referenceForCalculated => GetScope<IPreviousAocSteps>((Identity, StructureType.AocPresentValue)).Values
        .GroupBy(g => g.Novelty, 
            (g, val) => val.Last(aocStep => !ImportCalculationExtensions
                .ComputationHelper
                .CurrentPeriodCalculatedDataTypes
                .Any(dt => GetStorage().AocConfigurationByAocStep[aocStep].DataType.RepeatOnce().Contains(dt))));

    protected bool IsCalculatedAocStep => ImportCalculationExtensions.ComputationHelper
        .CurrentPeriodCalculatedDataTypes
        .Any(dt => GetStorage().AocConfigurationByAocStep[Identity.AocStep]
            .DataType.RepeatOnce().Contains(dt));

    IEnumerable<AocStep> Values => (
        IsCalculatedAocStep,
        ImportCalculationExtensions.ComputationHelper.ReferenceAocSteps.TryGetValue(Identity.AocStep, out var CustomDefinedReferenceAocStep) //IsCustomDefined
    ) switch
    {
        (true, false) => referenceForCalculated.Any(x => x.Novelty == Novelties.C) ? referenceForCalculated.Where(x => x.Novelty == Novelties.C) : referenceForCalculated,
        (true, true) => CustomDefinedReferenceAocStep,
        (false, _) => Identity.AocStep.RepeatOnce(),
    };
}