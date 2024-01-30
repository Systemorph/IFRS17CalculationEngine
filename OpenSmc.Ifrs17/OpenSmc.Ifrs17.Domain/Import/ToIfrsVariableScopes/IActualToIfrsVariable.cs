using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface IActualToIfrsVariable : IScope<ImportIdentity, ImportStorage>
{
    IEnumerable<IfrsVariable> Actual => Identity.AocType == AocTypes.CF && Identity.Novelty == Novelties.C
        ? GetScope<IActual>(Identity).Actuals.Select(written =>
            new IfrsVariable
            {
                EstimateType = written.EstimateType,
                DataNode = Identity.DataNode,
                AocType = Identity.AocType,
                Novelty = Identity.Novelty,
                AccidentYear = written.AccidentYear,
                AmountType = written.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(written.Value, Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            })
        : Enumerable.Empty<IfrsVariable>();

    IEnumerable<IfrsVariable> AdvanceActual => GetStorage().GetAllAocSteps(StructureType.AocAccrual).Contains(Identity.AocStep)
        ? GetScope<IAdvanceActual>(Identity).Actuals.Select(advance =>
            new IfrsVariable
            {
                EstimateType = advance.EstimateType,
                DataNode = Identity.DataNode,
                AocType = Identity.AocType,
                Novelty = Identity.Novelty,
                AccidentYear = advance.AccidentYear,
                AmountType = advance.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(advance.Value, Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            })
        : Enumerable.Empty<IfrsVariable>();

    IEnumerable<IfrsVariable> OverdueActual => GetStorage().GetAllAocSteps(StructureType.AocAccrual).Contains(Identity.AocStep)
        ? GetScope<IOverdueActual>(Identity).Actuals.Select(overdue =>
            new IfrsVariable
            {
                EstimateType = overdue.EstimateType,
                DataNode = Identity.DataNode,
                AocType = Identity.AocType,
                Novelty = Identity.Novelty,
                AccidentYear = overdue.AccidentYear,
                AmountType = overdue.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(overdue.Value, Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            })
        : Enumerable.Empty<IfrsVariable>();
}