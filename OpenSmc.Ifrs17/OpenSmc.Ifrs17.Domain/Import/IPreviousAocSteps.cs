using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IPreviousAocSteps : IScope<(ImportIdentity Id, StructureType AocStructure), ImportStorage>
{   
    private int AocStepOrder => GetStorage().AocConfigurationByAocStep[Identity.Id.AocStep].Order;

    private IEnumerable<AocStep> aocChainSteps => GetStorage().GetAllAocSteps(Identity.AocStructure);
    IEnumerable<AocStep> Values => aocChainSteps.Contains(Identity.Id.AocStep)
        ? GetScope<IGetIdentities>(Identity.Id.DataNode).AocSteps
            .Where(aoc => aocChainSteps.Contains(aoc) && GetStorage().AocConfigurationByAocStep[aoc].Order < AocStepOrder && 
                          (Identity.Id.Novelty != Novelties.C ? aoc.Novelty == Identity.Id.Novelty : true) )
            .OrderBy(aoc => GetStorage().AocConfigurationByAocStep[aoc].Order)
        : Enumerable.Empty<AocStep>();
}