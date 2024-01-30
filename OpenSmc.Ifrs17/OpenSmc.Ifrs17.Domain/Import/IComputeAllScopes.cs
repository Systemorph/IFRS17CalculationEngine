using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.IfrsVarsComputations;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IComputeAllScopes: IScope<string, ImportStorage>
{
    private IEnumerable<ImportIdentity> identities => Enumerable.Range(0, GetStorage().GetProjectionCount(Identity))
        .SelectMany(projectionPeriod => GetScope<IGetIdentities>(Identity).Identities.Select(id => id with { ProjectionPeriod = projectionPeriod}));

    IEnumerable<IfrsVariable> CalculatedIfrsVariables => identities.SelectMany(identity => 
        GetStorage().ImportFormat switch {
            ImportFormats.Actual   => GetScope<IComputeIfrsVarsIActuals>(identity).CalculatedIfrsVariables,
            ImportFormats.Cashflow => GetScope<IComputeIfrsVarsCashflows>(identity).CalculatedIfrsVariables,
            ImportFormats.Opening  => GetScope<IComputeIfrsVarsOpenings>(identity).CalculatedIfrsVariables,
            _ => Enumerable.Empty<IfrsVariable>(),
        }).AggregateProjections().Select(ifrsVariable => ifrsVariable with {Partition = GetStorage().TargetPartition});
}