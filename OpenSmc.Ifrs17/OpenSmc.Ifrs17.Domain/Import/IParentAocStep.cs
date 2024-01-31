using OpenSmc.Collections;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;


namespace OpenSmc.Ifrs17.Domain.Import;

public interface IParentAocStep : IScope<(ImportIdentity Id, string AmountType, StructureType AocStructure), ImportStorage>
{
    private IEnumerable<AocStep> CalculatedAocStep => GetStorage().AocConfigurationByAocStep.Where(kvp => kvp.Value.DataType.Contains(DataType.Calculated)).Select(kvp => kvp.Key);
    
    private IEnumerable<AocStep> TelescopicStepToBeRemoved => Identity.AmountType == AmountTypes.CDR ? Enumerable.Empty<AocStep>() : GetStorage().AocConfigurationByAocStep.Where(kvp => kvp.Value.AocType == AocTypes.CRU).Select(kvp => kvp.Key);
    private IEnumerable<AocStep> PreviousAocStepsNotCalculated => GetScope<IPreviousAocSteps>((Identity.Id, Identity.AocStructure)).Values.Where(aoc => !CalculatedAocStep.Concat(TelescopicStepToBeRemoved).Contains(aoc));
    private bool IsFirstCombinedStep => Identity.Id.Novelty == Novelties.C && !PreviousAocStepsNotCalculated.Any(aoc => aoc.Novelty == Novelties.C);
    private bool IsCalculatedStep => CalculatedAocStep.Contains(Identity.Id.AocStep);

    IEnumerable<AocStep> Values => (Identity.Id.AocType == AocTypes.BOP || IsCalculatedStep, IsFirstCombinedStep) switch {
        (true, _ ) => Enumerable.Empty<AocStep>(),
        (false, true) => PreviousAocStepsNotCalculated.GroupBy(g => g.Novelty, (g, val) => val.Last()),
        (false, false) => PreviousAocStepsNotCalculated.Last(aoc => aoc.Novelty == Identity.Id.Novelty).RepeatOnce(),
    };
}