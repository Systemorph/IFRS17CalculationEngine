using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.LossRecoveryComponent;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface ITmToIfrsVariable : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<ITmToIfrsVariable>(s => s.WithApplicability<EmptyITmIfrsVariable>(x =>
            !x.GetStorage().GetAllAocSteps(StructureType.AocTechnicalMargin).Contains(x.Identity.AocStep)));

    private string EconomicBasis => Identity.ValuationApproach == ValuationApproaches.VFA ? EconomicBases.C : EconomicBases.L;
    private IEnumerable<string> AmountTypesForTm => GetScope<ITechnicalMarginAmountType>((Identity, EstimateTypes.C)).Values;
    // TODO: we need to think how to define the logic on when to compute LC for PAA-LRC
    // private bool hasTechnicalMargin => GetStorage().ImportFormat switch {
    //     ImportFormats.Cashflow => GetStorage().GetRawVariables(Identity.DataNode).Any(x => x.EstimateType == EstimateTypes.RA || 
    //         (x.EstimateType == EstimateTypes.BE && amountTypesForTm.Contains(x.AmountType))),
    //     _ => GetStorage().GetIfrsVariables(Identity.DataNode).Any(x => !GetStorage().EstimateTypesByImportFormat[ImportFormats.IActual].Contains(x.EstimateType) && 
    //         amountTypesForTm.Contains(x.AmountType))
    // };

    IEnumerable<IfrsVariable> Csms => GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType == LiabilityTypes.LIC || Identity.ValuationApproach == ValuationApproaches.PAA
        ? Enumerable.Empty<IfrsVariable>()
        : GetScope<IContractualServiceMargin>(Identity).RepeatOnce()
            .Select(x => new IfrsVariable
            {
                EstimateType = x.EstimateType,
                DataNode = x.Identity.DataNode,
                AocType = x.Identity.AocType,
                Novelty = x.Identity.Novelty,
                EconomicBasis = EconomicBasis,
                Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            });

    IEnumerable<IfrsVariable> Loss => GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType == LiabilityTypes.LIC
        ? Enumerable.Empty<IfrsVariable>()
        : Identity.IsReinsurance
            ? GetScope<ILossRecoveryComponent>(Identity).RepeatOnce()
                .Select(x => new IfrsVariable
                {
                    EstimateType = x.EstimateType,
                    DataNode = x.Identity.DataNode,
                    AocType = x.Identity.AocType,
                    Novelty = x.Identity.Novelty,
                    EconomicBasis = EconomicBasis,
                    Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                    Partition = GetStorage().TargetPartition
                })
            : GetScope<ILossComponent>(Identity).RepeatOnce()
                .Select(x => new IfrsVariable
                {
                    EstimateType = x.EstimateType,
                    DataNode = x.Identity.DataNode,
                    AocType = x.Identity.AocType,
                    Novelty = x.Identity.Novelty,
                    EconomicBasis = EconomicBasis,
                    Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                    Partition = GetStorage().TargetPartition
                });

    IEnumerable<IfrsVariable> AmortizationFactor => Identity.AocType == AocTypes.AM && Loss.Concat(Csms).Where(x => x.Values != null).Any(x => Math.Abs(x.Values.GetValidElement(Identity.ProjectionPeriod)) > Consts.Precision)
        && GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType == LiabilityTypes.LRC
            ? GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).RepeatOnce()
                .Select(x => new IfrsVariable
                {
                    EstimateType = x.EstimateType,
                    DataNode = x.Identity.Id.DataNode,
                    AocType = x.Identity.Id.AocType,
                    Novelty = x.Identity.Id.Novelty,
                    AmountType = x.EffectiveAmountType,
                    EconomicBasis = x.EconomicBasis,
                    Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.Id.ProjectionPeriod),
                    Partition = GetStorage().TargetPartition
                })
            : Enumerable.Empty<IfrsVariable>();
}