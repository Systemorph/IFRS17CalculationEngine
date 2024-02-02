using OpenSmc.Collections;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface IRaToIfrsVariable : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IRaToIfrsVariable>(s => s.WithApplicability<IEmptyIRaIfrsVariable>(x =>
            !x.GetStorage().GetAllAocSteps(StructureType.AocPresentValue).Contains(x.Identity.AocStep)));

    IEnumerable<IfrsVariable> RaCurrent => GetScope<IRaCurrent>(Identity).RepeatOnce().SelectMany(x =>
        x.PresentValues.Select(pv =>
            new IfrsVariable
            {
                EconomicBasis = x.EconomicBasis,
                EstimateType = x.EstimateType,
                DataNode = x.Identity.DataNode,
                AocType = x.Identity.AocType,
                Novelty = x.Identity.Novelty,
                AccidentYear = pv.AccidentYear,
                AmountType = pv.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(pv.Value, x.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            }));

    IEnumerable<IfrsVariable> RaLocked => GetScope<IRaLocked>(Identity).RepeatOnce().SelectMany(x =>
        x.PresentValues.Select(pv =>
            new IfrsVariable
            {
                EconomicBasis = x.EconomicBasis,
                EstimateType = x.EstimateType,
                DataNode = x.Identity.DataNode,
                AocType = x.Identity.AocType,
                Novelty = x.Identity.Novelty,
                AccidentYear = pv.AccidentYear,
                AmountType = pv.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(pv.Value, x.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            }));
}