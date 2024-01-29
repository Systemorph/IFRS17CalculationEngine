using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IPvToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IPvToIfrsVariable>(s => s.WithApplicability<IEmptyIPvIfrsVariable>(x => 
            !x.GetStorage().GetAllAocSteps(StructureType.AocPresentValue).Contains(x.Identity.AocStep)));

    IEnumerable<IfrsVariable> PvLocked => GetScope<IPvLocked>(Identity).RepeatOnce().SelectMany(x =>
        x.PresentValues.Select(pv =>
            new IfrsVariable{ EconomicBasis = x.EconomicBasis, 
                EstimateType = x.EstimateType, 
                DataNode = x.Identity.DataNode, 
                AocType = x.Identity.AocType, 
                Novelty = x.Identity.Novelty, 
                AccidentYear = pv.AccidentYear,
                AmountType = pv.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(pv.Value, x.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition }));
    IEnumerable<IfrsVariable> PvCurrent => GetScope<IPvCurrent>(Identity).RepeatOnce().SelectMany(x => 
        x.PresentValues.Select(pv =>
            new IfrsVariable{ EconomicBasis = x.EconomicBasis, 
                EstimateType = x.EstimateType, 
                DataNode = x.Identity.DataNode, 
                AocType = x.Identity.AocType, 
                Novelty = x.Identity.Novelty, 
                AccidentYear = pv.AccidentYear,
                AmountType = pv.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(pv.Value, x.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition }));
}