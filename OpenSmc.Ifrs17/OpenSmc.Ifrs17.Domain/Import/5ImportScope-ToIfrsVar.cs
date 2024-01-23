using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface PvToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
  static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<PvToIfrsVariable>(s => s.WithApplicability<EmptyPvIfrsVariable>(x => 
          !x.GetStorage().GetAllAocSteps(StructureType.AocPresentValue).Contains(x.Identity.AocStep)));

  IEnumerable<IfrsVariable> PvLocked => GetScope<PvLocked>(Identity).RepeatOnce().SelectMany(x =>
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
  IEnumerable<IfrsVariable> PvCurrent => GetScope<PvCurrent>(Identity).RepeatOnce().SelectMany(x => 
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

public interface EmptyPvIfrsVariable: PvToIfrsVariable{
  IEnumerable<IfrsVariable> PvToIfrsVariable.PvLocked => Enumerable.Empty<IfrsVariable>();
  IEnumerable<IfrsVariable> PvToIfrsVariable.PvCurrent => Enumerable.Empty<IfrsVariable>();
}



public interface NominalToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
      builder.ForScope<NominalToIfrsVariable>(s => s.WithApplicability<EmptyNominalToIfrsVariable>(x => 
          !x.GetStorage().GetAllAocSteps(StructureType.AocPresentValue).Contains(x.Identity.AocStep)));

    IEnumerable<IfrsVariable> CumulatedNominal => GetScope<CumulatedNominalBE>(Identity).RepeatOnce().SelectMany(x => 
        x.PresentValues.Select(pv => 
            new IfrsVariable{ EconomicBasis = x.EconomicBasis, 
                              EstimateType = x.EstimateType, 
                              DataNode = x.Identity.DataNode, 
                              AocType = x.Identity.AocType, 
                              Novelty = x.Identity.Novelty, 
                              AccidentYear = pv.AccidentYear,
                              AmountType = pv.AmountType,
                              Values = ImportCalculationExtensions.SetProjectionValue(pv.Value, x.Identity.ProjectionPeriod),
                              Partition = GetStorage().TargetPartition}))
    .Concat(GetScope<CumulatedNominalRA>(Identity).RepeatOnce().SelectMany(x => 
        x.PresentValues.Select(pv => 
            new IfrsVariable{ EconomicBasis = x.EconomicBasis, 
                              EstimateType = x.EstimateType, 
                              DataNode = x.Identity.DataNode, 
                              AocType = x.Identity.AocType, 
                              Novelty = x.Identity.Novelty, 
                              AccidentYear = pv.AccidentYear,
                              AmountType = pv.AmountType,
                              Values = ImportCalculationExtensions.SetProjectionValue(pv.Value, x.Identity.ProjectionPeriod),
                              Partition = GetStorage().TargetPartition})));
}

public interface EmptyNominalToIfrsVariable: NominalToIfrsVariable{
    IEnumerable<IfrsVariable> NominalToIfrsVariable.CumulatedNominal => Enumerable.Empty<IfrsVariable>();
}


public interface RaToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
  static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<RaToIfrsVariable>(s => s.WithApplicability<EmptyRaIfrsVariable>(x => 
            !x.GetStorage().GetAllAocSteps(StructureType.AocPresentValue).Contains(x.Identity.AocStep)));
    
    IEnumerable<IfrsVariable> RaCurrent => GetScope<RaCurrent>(Identity).RepeatOnce().SelectMany(x => 
        x.PresentValues.Select(pv => 
            new IfrsVariable{ EconomicBasis = x.EconomicBasis, 
                              EstimateType = x.EstimateType, 
                              DataNode = x.Identity.DataNode, 
                              AocType = x.Identity.AocType, 
                              Novelty = x.Identity.Novelty, 
                              AccidentYear = pv.AccidentYear,
                              AmountType = pv.AmountType,
                              Values = ImportCalculationExtensions.SetProjectionValue(pv.Value, x.Identity.ProjectionPeriod),
                              Partition = GetStorage().TargetPartition}));
                        
    IEnumerable<IfrsVariable> RaLocked => GetScope<RaLocked>(Identity).RepeatOnce().SelectMany(x => 
        x.PresentValues.Select(pv => 
            new IfrsVariable{ EconomicBasis = x.EconomicBasis, 
                              EstimateType = x.EstimateType, 
                              DataNode = x.Identity.DataNode, 
                              AocType = x.Identity.AocType, 
                              Novelty = x.Identity.Novelty, 
                              AccidentYear = pv.AccidentYear,
                              AmountType = pv.AmountType,
                              Values = ImportCalculationExtensions.SetProjectionValue(pv.Value, x.Identity.ProjectionPeriod),
                              Partition = GetStorage().TargetPartition}));
}

public interface EmptyRaIfrsVariable: RaToIfrsVariable{
    IEnumerable<IfrsVariable> RaToIfrsVariable.RaCurrent => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> RaToIfrsVariable.RaLocked => Enumerable.Empty<IfrsVariable>();
}


public interface ActualToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
    IEnumerable<IfrsVariable> Actual => Identity.AocType == AocTypes.CF && Identity.Novelty == Novelties.C 
      ? GetScope<Actual>(Identity).Actuals.Select(written => 
      new IfrsVariable{ EstimateType = written.EstimateType,
                        DataNode = Identity.DataNode,
                        AocType = Identity.AocType,
                        Novelty = Identity.Novelty,
                        AccidentYear = written.AccidentYear,
                        AmountType = written.AmountType,
                        Values = ImportCalculationExtensions.SetProjectionValue(written.Value, Identity.ProjectionPeriod),
                        Partition = GetStorage().TargetPartition })
      : Enumerable.Empty<IfrsVariable>();
                        
    IEnumerable<IfrsVariable> AdvanceActual => GetStorage().GetAllAocSteps(StructureType.AocAccrual).Contains(Identity.AocStep)
      ? GetScope<AdvanceActual>(Identity).Actuals.Select(advance => 
      new IfrsVariable{ EstimateType = advance.EstimateType,
                        DataNode = Identity.DataNode,
                        AocType = Identity.AocType,
                        Novelty = Identity.Novelty,
                        AccidentYear = advance.AccidentYear,
                        AmountType = advance.AmountType,
                        Values = ImportCalculationExtensions.SetProjectionValue(advance.Value, Identity.ProjectionPeriod),
                        Partition = GetStorage().TargetPartition })
      : Enumerable.Empty<IfrsVariable>();

   IEnumerable<IfrsVariable> OverdueActual => GetStorage().GetAllAocSteps(StructureType.AocAccrual).Contains(Identity.AocStep)
      ? GetScope<OverdueActual>(Identity).Actuals.Select(overdue => 
      new IfrsVariable{ EstimateType = overdue.EstimateType,
                        DataNode = Identity.DataNode,
                        AocType = Identity.AocType,
                        Novelty = Identity.Novelty,
                        AccidentYear = overdue.AccidentYear,
                        AmountType = overdue.AmountType,
                        Values = ImportCalculationExtensions.SetProjectionValue(overdue.Value, Identity.ProjectionPeriod),
                        Partition = GetStorage().TargetPartition })
      : Enumerable.Empty<IfrsVariable>();
}


public interface DeferrableToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
    protected string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);
    private int timeStep => GetStorage().GetTimeStep(Identity.ProjectionPeriod); 

    IEnumerable<IfrsVariable> Deferrable => EconomicBasis switch {
        EconomicBases.N => Enumerable.Range(0, timeStep).SelectMany(shift => 
            GetScope<NominalDeferrable>((Identity, shift)).RepeatOnce()
                .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
                          EconomicBasis = EconomicBases.N,
                          DataNode = x.Identity.Id.DataNode,
                          AocType = x.Identity.Id.AocType,
                          Novelty = x.Identity.Id.Novelty,
                          AccidentYear = shift,
                          Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.Id.ProjectionPeriod),
                          Partition = GetStorage().TargetPartition })),
        _ => GetScope<DiscountedDeferrable>(Identity).RepeatOnce()
                .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
                          EconomicBasis = x.EconomicBasis,
                          DataNode = x.Identity.DataNode,
                          AocType = x.Identity.AocType,
                          Novelty = x.Identity.Novelty,
                          Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                          Partition = GetStorage().TargetPartition }),
    };

    private IEnumerable<IfrsVariable> amortizationStep => Deferrable.Where(iv => iv.Values != null).Where(iv => Math.Abs(iv.Values.GetValidElement(Identity.ProjectionPeriod)) > Consts.Precision);

    IEnumerable<IfrsVariable> DeferrableAmFactor => (Identity.AocType, amortizationStep.Any(), EconomicBasis) switch {
        (AocTypes.AM, true, EconomicBases.N) => amortizationStep.Select(x => x.AccidentYear.Value).SelectMany(shift => 
            GetScope<CurrentPeriodAmortizationFactor>((Identity, AmountTypes.DAE, shift), o => o.WithContext(EconomicBases.N)).RepeatOnce() //hardcoded AmountType: DAE for pattern
                .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
                          EconomicBasis = EconomicBases.N,
                          DataNode = x.Identity.Id.DataNode,
                          AocType = Identity.AocType,
                          Novelty = Identity.Novelty,
                          AmountType = x.EffectiveAmountType,
                          AccidentYear = shift,
                          Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.Id.ProjectionPeriod),
                          Partition = GetStorage().TargetPartition })),
        (AocTypes.AM, true, _) => GetScope<DiscountedAmortizationFactorForDeferrals>(Identity, o => o.WithContext(EconomicBasis)).RepeatOnce()
            .Select(x => new IfrsVariable{ EstimateType = EstimateTypes.F,
                          EconomicBasis = EconomicBasis,
                          DataNode = x.Identity.DataNode,
                          AocType = x.Identity.AocType,
                          Novelty = x.Identity.Novelty,
                          AmountType = x.EffectiveAmountType,
                          Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                          Partition = GetStorage().TargetPartition }),
        (_) => Enumerable.Empty<IfrsVariable>(),
    };
}


public interface RevenueToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<RevenueToIfrsVariable>(s => s
            .WithApplicability<EmptyRevenue>(x => !(x.Identity.ValuationApproach == ValuationApproaches.PAA && x.GetStorage().DataNodeDataBySystemName[x.Identity.DataNode].LiabilityType == LiabilityTypes.LRC)));

    protected string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);
    private int timeStep => GetStorage().GetTimeStep(Identity.ProjectionPeriod); 

    IEnumerable<IfrsVariable> Revenue => GetScope<PremiumRevenue>(Identity).RepeatOnce()
            .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
                          EconomicBasis = x.EconomicBasis,
                          DataNode = x.Identity.DataNode,
                          AocType = x.Identity.AocType,
                          Novelty = x.Identity.Novelty,
                          Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                          Partition = GetStorage().TargetPartition });
    
    private bool hasAmortizationStep => Revenue.Where(iv => iv.Values != null).Any(iv => Math.Abs(iv.Values.GetValidElement(Identity.ProjectionPeriod)) > Consts.Precision);

    IEnumerable<IfrsVariable> RevenueAmFactor =>  Identity.AocType == AocTypes.AM && hasAmortizationStep
        ? GetScope<DiscountedAmortizationFactorForRevenues>(Identity, o => o.WithContext(EconomicBasis)).RepeatOnce()
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
public interface EmptyRevenue : RevenueToIfrsVariable{
    IEnumerable<IfrsVariable> RevenueToIfrsVariable.Revenue => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> RevenueToIfrsVariable.RevenueAmFactor => Enumerable.Empty<IfrsVariable>();
}


public interface EaForPremiumToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
  private string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);
  IEnumerable<IfrsVariable> BeEAForPremium => Identity.AocType == AocTypes.CF && GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType != LiabilityTypes.LIC && !Identity.IsReinsurance
    ? GetScope<BeExperienceAdjustmentForPremium>(Identity, o => o.WithContext(EconomicBasis)).RepeatOnce()
      .Select(sc => new IfrsVariable{ EstimateType = sc.EstimateType, 
                                      DataNode = sc.Identity.DataNode, 
                                      AocType = sc.Identity.AocType, 
                                      Novelty = sc.Identity.Novelty, 
                                      EconomicBasis = sc.EconomicBasis,
                                      AmountType = sc.AmountType,
                                      Values = ImportCalculationExtensions.SetProjectionValue(sc.Value, sc.Identity.ProjectionPeriod),
                                      Partition = sc.GetStorage().TargetPartition })
    : Enumerable.Empty<IfrsVariable>();
    
  IEnumerable<IfrsVariable> ActEAForPremium => Identity.AocType == AocTypes.CF && GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType != LiabilityTypes.LIC && !Identity.IsReinsurance
    ? GetScope<ActualExperienceAdjustmentOnPremium>(Identity).RepeatOnce()
        .Select(sc => new IfrsVariable{ EstimateType = sc.EstimateType, 
                         DataNode = sc.Identity.DataNode, 
                         AocType = sc.Identity.AocType, 
                         Novelty = sc.Identity.Novelty, 
                         AmountType = sc.AmountType,
                         Values = ImportCalculationExtensions.SetProjectionValue(sc.Value, sc.Identity.ProjectionPeriod),
                         Partition = GetStorage().TargetPartition })
    : Enumerable.Empty<IfrsVariable>();
}


public interface TmToIfrsVariable: IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<TmToIfrsVariable>(s => s.WithApplicability<EmptyTmIfrsVariable>(x =>
            !x.GetStorage().GetAllAocSteps(StructureType.AocTechnicalMargin).Contains(x.Identity.AocStep)));

    private string economicBasis => Identity.ValuationApproach == ValuationApproaches.VFA ? EconomicBases.C : EconomicBases.L;
    private IEnumerable<string> amountTypesForTm => GetScope<TechnicalMarginAmountType>((Identity, EstimateTypes.C)).Values;
    // TODO: we need to think how to define the logic on when to compute LC for PAA-LRC
    // private bool hasTechnicalMargin => GetStorage().ImportFormat switch {
    //     ImportFormats.Cashflow => GetStorage().GetRawVariables(Identity.DataNode).Any(x => x.EstimateType == EstimateTypes.RA || 
    //         (x.EstimateType == EstimateTypes.BE && amountTypesForTm.Contains(x.AmountType))),
    //     _ => GetStorage().GetIfrsVariables(Identity.DataNode).Any(x => !GetStorage().EstimateTypesByImportFormat[ImportFormats.Actual].Contains(x.EstimateType) && 
    //         amountTypesForTm.Contains(x.AmountType))
    // };

    IEnumerable<IfrsVariable> Csms => GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType == LiabilityTypes.LIC || Identity.ValuationApproach == ValuationApproaches.PAA
        ? Enumerable.Empty<IfrsVariable>()
        : GetScope<ContractualServiceMargin>(Identity).RepeatOnce()
          .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
               DataNode = x.Identity.DataNode,
               AocType = x.Identity.AocType,
               Novelty = x.Identity.Novelty,
               EconomicBasis = economicBasis,
               Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
               Partition = GetStorage().TargetPartition
            });

     IEnumerable<IfrsVariable> Loss => GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType == LiabilityTypes.LIC
        ? Enumerable.Empty<IfrsVariable>()
        : Identity.IsReinsurance 
           ? GetScope<LossRecoveryComponent>(Identity).RepeatOnce()
               .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
                                              DataNode = x.Identity.DataNode,
                                              AocType = x.Identity.AocType,
                                              Novelty = x.Identity.Novelty,
                                              EconomicBasis = economicBasis,
                                              Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                                              Partition = GetStorage().TargetPartition
                                           })
           : GetScope<LossComponent>(Identity).RepeatOnce()
               .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
                                              DataNode = x.Identity.DataNode,
                                              AocType = x.Identity.AocType,
                                              Novelty = x.Identity.Novelty,
                                              EconomicBasis = economicBasis,
                                              Values = ImportCalculationExtensions.SetProjectionValue(x.Value, x.Identity.ProjectionPeriod),
                                              Partition = GetStorage().TargetPartition
                                           });
    
    IEnumerable<IfrsVariable> AmortizationFactor =>  Identity.AocType == AocTypes.AM && Loss.Concat(Csms).Where(x => x.Values != null).Any(x => Math.Abs(x.Values.GetValidElement(Identity.ProjectionPeriod)) > Consts.Precision)
        && GetStorage().DataNodeDataBySystemName[Identity.DataNode].LiabilityType == LiabilityTypes.LRC
        ? GetScope<CurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(economicBasis)).RepeatOnce()
            .Select(x => new IfrsVariable{ EstimateType = x.EstimateType,
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

public interface EmptyTmIfrsVariable: TmToIfrsVariable {
    IEnumerable<IfrsVariable> TmToIfrsVariable.Csms => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> TmToIfrsVariable.Loss => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> TmToIfrsVariable.AmortizationFactor => Enumerable.Empty<IfrsVariable>();
}



