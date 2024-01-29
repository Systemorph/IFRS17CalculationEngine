using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IReferenceAocStep : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IReferenceAocStep>(s => s.WithApplicability<IReferenceAocStepForProjections>(x => x.Identity.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection));

    protected IEnumerable<AocStep> referenceForCalculated => GetScope<IPreviousAocSteps>((Identity, StructureType.AocPresentValue)).Values
        .GroupBy(g => g.Novelty, (g, val) => val.Last(aocStep => !ImportCalculationExtensions.ComputationHelper.CurrentPeriodCalculatedDataTypes.Any(dt => GetStorage().AocConfigurationByAocStep[aocStep].DataType.Contains(dt))));
                
    protected bool IsCalculatedAocStep => ImportCalculationExtensions.ComputationHelper.CurrentPeriodCalculatedDataTypes.Any(dt => GetStorage().AocConfigurationByAocStep[Identity.AocStep].DataType.Contains(dt));
    
    IEnumerable<AocStep> Values => (
        IsCalculatedAocStep, 
        ImportCalculationExtensions.ComputationHelper.ReferenceAocSteps.TryGetValue(Identity.AocStep, out var CustomDefinedReferenceAocStep) //IsCustomDefined
    ) switch {
        (true, false) => referenceForCalculated.Any(x => x.Novelty == Novelties.C) ? referenceForCalculated.Where(x => x.Novelty == Novelties.C) : referenceForCalculated,
        (true, true) => CustomDefinedReferenceAocStep,
        (false, _) => Identity.AocStep.RepeatOnce(),
    };
}