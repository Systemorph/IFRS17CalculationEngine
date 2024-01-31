using OpenSmc.Collections;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IReferenceAocStepForProjections : IReferenceAocStep
{
    private bool IsInforce => Identity.Novelty == Novelties.I;

    IEnumerable<AocStep> IReferenceAocStep.Values => (
        IsCalculatedAocStep, 
        ImportCalculationExtensions.ComputationHelper.ReferenceAocSteps.TryGetValue(Identity.AocStep, out var CustomDefinedReferenceAocStep), //IsCustomDefined
        IsInforce
    ) switch {
        (true, false, false) => referenceForCalculated.Any(x => x.Novelty == Novelties.C) ? referenceForCalculated.Where(x => x.Novelty == Novelties.C) : referenceForCalculated,
        (true, false, true) or (false, false, true) => new []{new AocStep(AocTypes.CL, Novelties.C)},
        (true, true, _) or (false, true, true) => CustomDefinedReferenceAocStep,
        (false, true, false) or (false, false, false) => Identity.AocStep.RepeatOnce(),
    };
}