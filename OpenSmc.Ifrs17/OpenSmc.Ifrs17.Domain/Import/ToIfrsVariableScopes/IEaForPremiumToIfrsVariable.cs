using OpenSmc.Collections;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;
using OpenSmc.Ifrs17.Domain.Import.ActualExperienceAdjustmentOnPremium;
using OpenSmc.Ifrs17.Domain.Import.ExperienceAdjustmentForPremium;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface IEaForPremiumToIfrsVariable : IScope<ImportIdentity, ImportStorage>
{
    private string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);
    IEnumerable<IfrsVariable> BeEaForPremium => Identity.AocType == AocTypes.CF && GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType != LiabilityTypes.LIC && !Identity.IsReinsurance
        ? GetScope<IBeExperienceAdjustmentForPremium>(Identity, o => o.WithContext(EconomicBasis)).RepeatOnce()
            .Select(sc => new IfrsVariable
            {
                EstimateType = sc.EstimateType,
                DataNode = sc.Identity.DataNode,
                AocType = sc.Identity.AocType,
                Novelty = sc.Identity.Novelty,
                EconomicBasis = sc.EconomicBasis,
                AmountType = sc.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(sc.Value, sc.Identity.ProjectionPeriod),
                Partition = sc.GetStorage().TargetPartition
            })
        : Enumerable.Empty<IfrsVariable>();

    IEnumerable<IfrsVariable> ActEAForPremium => Identity.AocType == AocTypes.CF && GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType != LiabilityTypes.LIC && !Identity.IsReinsurance
        ? GetScope<IActualExperienceAdjustmentOnPremium>(Identity).RepeatOnce()
            .Select(sc => new IfrsVariable
            {
                EstimateType = sc.EstimateType,
                DataNode = sc.Identity.DataNode,
                AocType = sc.Identity.AocType,
                Novelty = sc.Identity.Novelty,
                AmountType = sc.AmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(sc.Value, sc.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            })
        : Enumerable.Empty<IfrsVariable>();
}