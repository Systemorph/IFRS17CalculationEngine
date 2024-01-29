using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IRevenueToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IRevenueToIfrsVariable>(s => s
            .WithApplicability<IEmptyIRevenue>(x => !(x.Identity.ValuationApproach == ValuationApproaches.PAA && x.GetStorage().DataNodeDataBySystemName[x.Identity.DataNode].LiabilityType == LiabilityTypes.LRC)));

    protected string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);
    private int TimeStep => GetStorage().GetTimeStep(Identity.ProjectionPeriod); 

    IEnumerable<IfrsVariable> Revenue => GetScope<IPremiumRevenue>(Identity).RepeatOnce()
        .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
            EconomicBasis = x.EconomicBasis,
            DataNode = x.Identity.DataNode,
            AocType = x.Identity.AocType,
            Novelty = x.Identity.Novelty,
            Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
            Partition = GetStorage().TargetPartition });
    
    private bool HasAmortizationStep => Revenue.Where(iv => iv.Values != null).Any(iv => Math.Abs(iv.Values.GetValidElement(Identity.ProjectionPeriod)) > Consts.Precision);

    IEnumerable<IfrsVariable> RevenueAmFactor =>  Identity.AocType == AocTypes.AM && HasAmortizationStep
        ? GetScope<IDiscountedAmortizationFactorForRevenues>(Identity, o => o.WithContext(EconomicBasis)).RepeatOnce()
            .Select(x => new IfrsVariable{ EstimateType = EstimateTypes.F,
                EconomicBasis = EconomicBasis,
                DataNode = x.Identity.DataNode,
                AocType = x.Identity.AocType,
                Novelty = x.Identity.Novelty,
                AmountType = x.EffectiveAmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition })
        : Enumerable.Empty<IfrsVariable>();
}