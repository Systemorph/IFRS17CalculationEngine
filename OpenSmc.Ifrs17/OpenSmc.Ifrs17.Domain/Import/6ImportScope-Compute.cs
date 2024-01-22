//#!import "5ImportScope-ToIfrsVar"


using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Scopes;

public interface ComputeIfrsVarsActuals : ActualToIfrsVariable, DeferrableToIfrsVariable, EaForPremiumToIfrsVariable, TmToIfrsVariable
{
    IEnumerable<IfrsVariable> CalculatedIfrsVariables => Actual.Concat(AdvanceActual).Concat(OverdueActual)
        .Concat(ActEAForPremium).Concat(Deferrable).Concat(Csms).Concat(Loss);
}


public interface ComputeIfrsVarsCashflows : PvToIfrsVariable, RaToIfrsVariable, DeferrableToIfrsVariable, EaForPremiumToIfrsVariable, 
    TmToIfrsVariable, NominalToIfrsVariable, RevenueToIfrsVariable, ActualToIfrsVariable
{
    IEnumerable<IfrsVariable> amortizationFactors => Identity.ValuationApproach switch {
        ValuationApproaches.PAA => AmortizationFactor.Union(DeferrableAmFactor, EqualityComparer<IfrsVariable>.Instance),
//.Union(RevenueAmFactor, EqualityComparer<IfrsVariable>.Instance),//No need to RevenueAmFactor as long as Revenue depends only on Cashflow
        _ => AmortizationFactor.Union(DeferrableAmFactor, EqualityComparer<IfrsVariable>.Instance),
    };
    
    IEnumerable<IfrsVariable> CalculatedIfrsVariables => ( (Identity.ValuationApproach, GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType) switch {
        (ValuationApproaches.VFA, _) => PvCurrent.Concat(CumulatedNominal).Concat(RaCurrent).Concat(amortizationFactors)
            .Concat(BeEAForPremium).Concat(Deferrable).Concat(Csms).Concat(Loss),
        (ValuationApproaches.PAA, LiabilityTypes.LIC) => PvLocked.Concat(PvCurrent).Concat(CumulatedNominal).Concat(RaCurrent).Concat(RaLocked),                           
        (ValuationApproaches.PAA, LiabilityTypes.LRC) => PvLocked.Concat(CumulatedNominal).Concat(RaLocked).Concat(amortizationFactors).Concat(BeEAForPremium)
            .Concat(Deferrable).Concat(Loss).Concat(Revenue),
        _ => PvLocked.Concat(PvCurrent).Concat(CumulatedNominal).Concat(RaCurrent).Concat(RaLocked).Concat(amortizationFactors)
             .Concat(BeEAForPremium).Concat(Deferrable).Concat(Csms).Concat(Loss),
    } )
    // Adding Actuals.
    .Concat(Actual).Concat(AdvanceActual).Concat(OverdueActual).Concat(ActEAForPremium);
}


public interface ComputeIfrsVarsOpenings : ActualToIfrsVariable, DeferrableToIfrsVariable, TmToIfrsVariable, RevenueToIfrsVariable
{
    IEnumerable<IfrsVariable> CalculatedIfrsVariables => AdvanceActual.Concat(OverdueActual)
        .Concat(Deferrable).Concat(Csms).Concat(Loss).Concat(Revenue);
}


public interface ComputeAllScopes: IScope<string, ImportStorage>
{
    private IEnumerable<ImportIdentity> identities => Enumerable.Range(0, GetStorage().GetProjectionCount(Identity))
        .SelectMany(projectionPeriod => GetScope<GetIdentities>(Identity).Identities.Select(id => id with { ProjectionPeriod = projectionPeriod}));

   IEnumerable<IfrsVariable> CalculatedIfrsVariables => identities.SelectMany(identity => 
    GetStorage().ImportFormat switch {
            ImportFormats.Actual   => GetScope<ComputeIfrsVarsActuals>(identity).CalculatedIfrsVariables,
            ImportFormats.Cashflow => GetScope<ComputeIfrsVarsCashflows>(identity).CalculatedIfrsVariables,
            ImportFormats.Opening  => GetScope<ComputeIfrsVarsOpenings>(identity).CalculatedIfrsVariables,
            _ => Enumerable.Empty<IfrsVariable>(),
   }).AggregateProjections().Select(ifrsVariable => ifrsVariable with {Partition = GetStorage().TargetPartition});
}



