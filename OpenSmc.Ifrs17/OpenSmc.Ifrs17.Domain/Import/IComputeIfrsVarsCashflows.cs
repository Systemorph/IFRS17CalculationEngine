using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IComputeIfrsVarsCashflows : IPvToIfrsVariable, IRaToIfrsVariable, IDeferrableToIfrsVariable, IEaForPremiumToIfrsVariable, 
    ITmToIfrsVariable, INominalToIfrsVariable, IRevenueToIfrsVariable, IActualToIfrsVariable
{
    IEnumerable<IfrsVariable> AmortizationFactors => Identity.ValuationApproach switch {
        ValuationApproaches.PAA => AmortizationFactor.Union(DeferrableAmFactor, OpenSmc.Ifrs17.Domain.Utils.EqualityComparer<IfrsVariable>.Instance),
//.Union(RevenueAmFactor, EqualityComparer<IfrsVariable>.Instance),//No need to RevenueAmFactor as long as Revenue depends only on Cashflow
        _ => AmortizationFactor.Union(DeferrableAmFactor, OpenSmc.Ifrs17.Domain.Utils.EqualityComparer<IfrsVariable>.Instance),
    };
    
    IEnumerable<IfrsVariable> CalculatedIfrsVariables => ( (Identity.ValuationApproach, GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType) switch {
        (ValuationApproaches.VFA, _) => PvCurrent.Concat(CumulatedNominal).Concat(RaCurrent).Concat(AmortizationFactors)
            .Concat(BeEAForPremium).Concat(Deferrable).Concat(Csms).Concat(Loss),
        (ValuationApproaches.PAA, LiabilityTypes.LIC) => PvLocked.Concat(PvCurrent).Concat(CumulatedNominal).Concat(RaCurrent).Concat(RaLocked),                           
        (ValuationApproaches.PAA, LiabilityTypes.LRC) => PvLocked.Concat(CumulatedNominal).Concat(RaLocked).Concat(AmortizationFactors).Concat(BeEAForPremium)
            .Concat(Deferrable).Concat(Loss).Concat(Revenue),
        _ => PvLocked.Concat(PvCurrent).Concat(CumulatedNominal).Concat(RaCurrent).Concat(RaLocked).Concat(AmortizationFactors)
             .Concat(BeEAForPremium).Concat(Deferrable).Concat(Csms).Concat(Loss),
    } )
    // Adding Actuals.
    .Concat(Actual).Concat(AdvanceActual).Concat(OverdueActual).Concat(ActEAForPremium);
}