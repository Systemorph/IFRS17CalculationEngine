using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;
using OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface IDeferrableToIfrsVariable : IScope<ImportIdentity, ImportStorage>
{
    protected string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);
    private int TimeStep => GetStorage().GetTimeStep(Identity.ProjectionPeriod);

    IEnumerable<IfrsVariable> Deferrable => EconomicBasis switch
    {
        EconomicBases.N => Enumerable.Range(0, TimeStep).SelectMany(shift =>
            GetScope<INominalDeferrable>((Identity, shift)).RepeatOnce()
                .Select(x => new IfrsVariable
                {
                    EstimateType = x.EstimateType,
                    EconomicBasis = EconomicBases.N,
                    DataNode = x.Identity.Id.DataNode,
                    AocType = x.Identity.Id.AocType,
                    Novelty = x.Identity.Id.Novelty,
                    AccidentYear = shift,
                    Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.Id.ProjectionPeriod),
                    Partition = GetStorage().TargetPartition
                })),
        _ => GetScope<IDiscountedDeferrable>(Identity).RepeatOnce()
            .Select(x => new IfrsVariable
            {
                EstimateType = x.EstimateType,
                EconomicBasis = x.EconomicBasis,
                DataNode = x.Identity.DataNode,
                AocType = x.Identity.AocType,
                Novelty = x.Identity.Novelty,
                Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            }),
    };

    private IEnumerable<IfrsVariable> AmortizationStep => Deferrable.Where(iv => iv.Values != null).Where(iv => Math.Abs(iv.Values.GetValidElement(Identity.ProjectionPeriod)) > Consts.Precision);

    IEnumerable<IfrsVariable> DeferrableAmFactor => (Identity.AocType, AmortizationStep.Any(), EconomicBasis) switch
    {
        (AocTypes.AM, true, EconomicBases.N) => AmortizationStep.Select(x => x.AccidentYear.Value).SelectMany(shift =>
            GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.DAE, shift), o => o.WithContext(EconomicBases.N)).RepeatOnce() //hardcoded AmountType: DAE for pattern
                .Select(x => new IfrsVariable
                {
                    EstimateType = x.EstimateType,
                    EconomicBasis = EconomicBases.N,
                    DataNode = x.Identity.Id.DataNode,
                    AocType = Identity.AocType,
                    Novelty = Identity.Novelty,
                    AmountType = x.EffectiveAmountType,
                    AccidentYear = shift,
                    Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.Id.ProjectionPeriod),
                    Partition = GetStorage().TargetPartition
                })),
        (AocTypes.AM, true, _) => GetScope<IDiscountedAmortizationFactorForDeferrals>(Identity, o => o.WithContext(EconomicBasis)).RepeatOnce()
            .Select(x => new IfrsVariable
            {
                EstimateType = EstimateTypes.F,
                EconomicBasis = EconomicBasis,
                DataNode = x.Identity.DataNode,
                AocType = x.Identity.AocType,
                Novelty = x.Identity.Novelty,
                AmountType = x.EffectiveAmountType,
                Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                Partition = GetStorage().TargetPartition
            }),
        _ => Enumerable.Empty<IfrsVariable>(),
    };
}