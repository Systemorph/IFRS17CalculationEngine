#!import "ImportStorage"


public interface IModel : IMutableScopeWithStorage<ImportStorage>{}


public interface GetParsedAocSteps : IScope<string, ImportStorage>
{
    IEnumerable<AocStep> Values => GetStorage().GetRawVariables(Identity).Select(x => new AocStep(x.AocType, x.Novelty)).Distinct();
}


public interface GetIdentities : IScope<string, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<GetIdentities>(s => s.WithApplicability<GetCashflowIdentities>(x => x.GetStorage().ImportFormat == ImportFormats.Cashflow));
    
    protected IEnumerable<ImportIdentity> allIdentities => GetStorage().AocConfigurationByAocStep.Values.Select(x => new ImportIdentity {AocType = x.AocType, Novelty = x.Novelty, DataNode = Identity });

    IEnumerable<ImportIdentity> Identities => allIdentities.Select(id => id with { IsReinsurance = GetStorage().DataNodeDataBySystemName[id.DataNode].IsReinsurance,
                                                                                   ValuationApproach = GetStorage().DataNodeDataBySystemName[id.DataNode].ValuationApproach,
                                                                                   LiabilityType = GetStorage().DataNodeDataBySystemName[id.DataNode].LiabilityType });
    IEnumerable<AocStep> AocSteps => Identities.Select(id => id.AocStep).Distinct();
}

public interface GetCashflowIdentities : GetIdentities
{
    private bool isReinsurance => GetStorage().DataNodeDataBySystemName[Identity].IsReinsurance;
    private IEnumerable<ImportIdentity> ParsedIdentities => GetScope<GetParsedAocSteps>(Identity).Values.Select(aocStep => new ImportIdentity {AocType = aocStep.AocType, Novelty = aocStep.Novelty, DataNode = Identity});
    private IEnumerable<string> rawVariableNovelties => GetStorage().GetRawVariables(Identity).Select(rv => rv.Novelty).Concat(Novelties.C.RepeatOnce()).ToHashSet();
    private IEnumerable<AocStep> calculatedAocSteps => GetStorage().AocConfigurationByAocStep.Values.Where(x => ComputationHelper.CurrentPeriodCalculatedDataTypes.Any(y => x.DataType.Contains(y)) &&
        (!isReinsurance ? !ComputationHelper.ReinsuranceAocType.Contains(x.AocType) : true) && rawVariableNovelties.Contains(x.Novelty) 
        || x.DataType.Contains(DataType.CalculatedProjection) ).Select(x => new AocStep(x.AocType, x.Novelty));
    private IEnumerable<ImportIdentity> specialIdentities => calculatedAocSteps.Select(x => new ImportIdentity {AocType = x.AocType, Novelty = x.Novelty, DataNode = Identity })
        .Concat(GetStorage().AocConfigurationByAocStep.Values.Where(x => (!isReinsurance ? !ComputationHelper.ReinsuranceAocType.Contains(x.AocType) : true) && x.DataType.Contains(DataType.Calculated) && x.Novelty == Novelties.I)
        .Select(aocStep => new ImportIdentity{AocType = aocStep.AocType, Novelty = aocStep.Novelty, DataNode = Identity}));

    IEnumerable<ImportIdentity> GetIdentities.allIdentities => ParsedIdentities.Concat(specialIdentities).Distinct();       
}


public interface ValidAmountType : IScope<string, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
                builder.ForScope<ValidAmountType>(s => s.WithApplicability<BeAmountTypesFromIfrsVariables>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow ||
                                                                                                                x.GetStorage().IsSecondaryScope(x.Identity)));
    
    IEnumerable<string> BeAmountTypes => GetStorage().GetRawVariables(Identity)
        .Where(rv => rv.AmountType != null).Select(x => x.AmountType).Concat(
            GetStorage().DataNodeDataBySystemName[Identity].IsReinsurance 
                ? GetStorage().DataNodeDataBySystemName[Identity].LiabilityType == LiabilityTypes.LIC 
                    ? new []{AmountTypes.CDR} : new []{AmountTypes.CDRI, AmountTypes.CDR} 
                    : Enumerable.Empty<string>()).ToHashSet();
    
    IEnumerable<string> ActualAmountTypes => GetStorage().GetIfrsVariables(Identity)
        .Where(iv => GetStorage().EstimateTypesByImportFormat[ImportFormats.Actual].Contains(iv.EstimateType))
        .Select(x => x.AmountType).ToHashSet();

    IEnumerable<string> AllImportedAmountTypes => BeAmountTypes.Union(ActualAmountTypes).ToHashSet();
}
public interface BeAmountTypesFromIfrsVariables : ValidAmountType
{
    IEnumerable<string> ValidAmountType.BeAmountTypes => GetStorage().GetIfrsVariables(Identity)
        .Where(iv => GetStorage().EstimateTypesByImportFormat[ImportFormats.Cashflow].Contains(iv.EstimateType) && iv.AmountType != null)
        .Select(x => x.AmountType).ToHashSet();
}


public interface PreviousAocSteps : IScope<(ImportIdentity Id, StructureType AocStructure), ImportStorage>
{   
    private int aocStepOrder => GetStorage().AocConfigurationByAocStep[Identity.Id.AocStep].Order;
    private IEnumerable<AocStep> aocChainSteps => GetStorage().GetAllAocSteps(Identity.AocStructure);
    IEnumerable<AocStep> Values => aocChainSteps.Contains(Identity.Id.AocStep)
        ? GetScope<GetIdentities>(Identity.Id.DataNode).AocSteps
            .Where(aoc => aocChainSteps.Contains(aoc) && GetStorage().AocConfigurationByAocStep[aoc].Order < aocStepOrder && 
                          (Identity.Id.Novelty != Novelties.C ? aoc.Novelty == Identity.Id.Novelty : true) )
            .OrderBy(aoc => GetStorage().AocConfigurationByAocStep[aoc].Order)
        : Enumerable.Empty<AocStep>();
} 


public interface ParentAocStep : IScope<(ImportIdentity Id, string AmountType, StructureType AocStructure), ImportStorage>
{
    private IEnumerable<AocStep> CalculatedAocStep => GetStorage().AocConfigurationByAocStep.Where(kvp => kvp.Value.DataType.Contains(DataType.Calculated)).Select(kvp => kvp.Key);
    
    private IEnumerable<AocStep> TelescopicStepToBeRemoved => Identity.AmountType == AmountTypes.CDR ? Enumerable.Empty<AocStep>() : GetStorage().AocConfigurationByAocStep.Where(kvp => kvp.Value.AocType == AocTypes.CRU).Select(kvp => kvp.Key);
    private IEnumerable<AocStep> PreviousAocStepsNotCalculated => GetScope<PreviousAocSteps>((Identity.Id, Identity.AocStructure)).Values.Where(aoc => !CalculatedAocStep.Concat(TelescopicStepToBeRemoved).Contains(aoc));
    private bool IsFirstCombinedStep => Identity.Id.Novelty == Novelties.C && !PreviousAocStepsNotCalculated.Any(aoc => aoc.Novelty == Novelties.C);
    private bool IsCalculatedStep => CalculatedAocStep.Contains(Identity.Id.AocStep);

    IEnumerable<AocStep> Values => (Identity.Id.AocType == AocTypes.BOP || IsCalculatedStep, IsFirstCombinedStep) switch {
        (true, _ ) => Enumerable.Empty<AocStep>(),
        (false, true) => PreviousAocStepsNotCalculated.GroupBy(g => g.Novelty, (g, val) => val.Last()),
        (false, false) => PreviousAocStepsNotCalculated.Last(aoc => aoc.Novelty == Identity.Id.Novelty).RepeatOnce(),
    };
}


// The Reference AocStep from which the data (Nominal or PV) is retrieved to to compute the current AoC Step
public interface ReferenceAocStep : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
                builder.ForScope<ReferenceAocStep>(s => s.WithApplicability<ReferenceAocStepForProjections>(x => x.Identity.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection));

    protected IEnumerable<AocStep> referenceForCalculated => GetScope<PreviousAocSteps>((Identity, StructureType.AocPresentValue)).Values
        .GroupBy(g => g.Novelty, (g, val) => val.Last(aocStep => !ComputationHelper.CurrentPeriodCalculatedDataTypes.Any(dt => GetStorage().AocConfigurationByAocStep[aocStep].DataType.Contains(dt))));
                
    protected bool IsCalculatedAocStep => ComputationHelper.CurrentPeriodCalculatedDataTypes.Any(dt => GetStorage().AocConfigurationByAocStep[Identity.AocStep].DataType.Contains(dt));
    
    IEnumerable<AocStep> Values => (
        IsCalculatedAocStep, 
        ComputationHelper.ReferenceAocSteps.TryGetValue(Identity.AocStep, out var CustomDefinedReferenceAocStep) //IsCustomDefined
        ) switch {
            (true, false) => referenceForCalculated.Any(x => x.Novelty == Novelties.C) ? referenceForCalculated.Where(x => x.Novelty == Novelties.C) : referenceForCalculated,
            (true, true) => CustomDefinedReferenceAocStep,
            (false, _) => Identity.AocStep.RepeatOnce(),
            };
}

public interface ReferenceAocStepForProjections : ReferenceAocStep
{
    private bool IsInforce => Identity.Novelty == Novelties.I;

    IEnumerable<AocStep> ReferenceAocStep.Values => (
        IsCalculatedAocStep, 
        ComputationHelper.ReferenceAocSteps.TryGetValue(Identity.AocStep, out var CustomDefinedReferenceAocStep), //IsCustomDefined
        IsInforce
        ) switch {
            (true, false, false) => referenceForCalculated.Any(x => x.Novelty == Novelties.C) ? referenceForCalculated.Where(x => x.Novelty == Novelties.C) : referenceForCalculated,
            (true, false, true) or (false, false, true) => new []{new AocStep(AocTypes.CL, Novelties.C)},
            (true, true, _) or (false, true, true) => CustomDefinedReferenceAocStep,
            (false, true, false) or (false, false, false) => Identity.AocStep.RepeatOnce(),
            };
}



