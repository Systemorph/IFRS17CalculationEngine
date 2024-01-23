using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;
public interface BeExperienceAdjustmentForPremium : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<BeExperienceAdjustmentForPremium>(s => s
            .WithApplicability<DefaultValueBeExperienceAdjustmentForPremium>(x => x.Identity.AocType != AocTypes.CF)
            .WithApplicability<DefaultValueBeExperienceAdjustmentForPremium>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA && x.Identity.Novelty != Novelties.C)
            .WithApplicability<BeExperienceAdjustmentForPremiumForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA)
            );

    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => ImportCalculationExtensions.ComputationHelper.ExperienceAdjustEstimateTypeMapping[EstimateTypes.BE];
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] string EconomicBasis => GetContext();
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string AmountType => AmountTypes.PR;

    double Value => GetStorage().GetPremiumAllocationFactor(Identity) * 
        GetStorage().GetPremiums().Sum(pr => GetScope<PvAggregatedOverAccidentYear>((Identity, pr, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value);
}

public interface DefaultValueBeExperienceAdjustmentForPremium : BeExperienceAdjustmentForPremium{
    double BeExperienceAdjustmentForPremium.Value => default;
}

public interface BeExperienceAdjustmentForPremiumForPaa : BeExperienceAdjustmentForPremium {
    double BeExperienceAdjustmentForPremium.Value => GetScope<PremiumRevenue>(Identity with {AocType = AocTypes.AM, Novelty = Novelties.C}).Value;
}


public interface ActualExperienceAdjustmentOnPremium : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<ActualExperienceAdjustmentOnPremium>(s => s
            .WithApplicability<DefaultValueActualExperienceAdjustmentOnPremium>(x => x.Identity.AocType != AocTypes.CF)
            .WithApplicability<DefaultValueActualExperienceAdjustmentOnPremium>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA && x.Identity.Novelty != Novelties.C)
            .WithApplicability<ActualExperienceAdjustmentOnPremiumForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA));
    
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string AmountType => AmountTypes.PR;
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => ImportCalculationExtensions.ComputationHelper.ExperienceAdjustEstimateTypeMapping[EstimateTypes.A];
    
    double Value => GetStorage().GetPremiumAllocationFactor(Identity) * 
        GetStorage().GetPremiums().Sum(pr => GetScope<WrittenActual>((Identity, pr, EstimateTypes.A, (int?)null)).Value);
}

public interface DefaultValueActualExperienceAdjustmentOnPremium : ActualExperienceAdjustmentOnPremium{
    double ActualExperienceAdjustmentOnPremium.Value => default;
}

public interface ActualExperienceAdjustmentOnPremiumForPaa : ActualExperienceAdjustmentOnPremium{
    double ActualExperienceAdjustmentOnPremium.Value => GetScope<PremiumRevenue>(Identity with {AocType = AocTypes.AM, Novelty = Novelties.C}).Value;
}


public interface TechnicalMarginAmountType : IScope<(ImportIdentity Id, string EstimateType), ImportStorage>
{
    protected IEnumerable<string> amountTypesToExclude => (Identity.EstimateType, Identity.Id.ValuationApproach) switch {
        (EstimateTypes.LR, ValuationApproaches.PAA) => GetStorage().GetCoverageUnits().Concat(GetStorage().GetNonAttributableAmountType()).Concat(AmountTypes.CDR.RepeatOnce()).Concat(GetStorage().GetAttributableExpenses()).Concat(GetStorage().GetDeferrableExpenses()).Concat(GetStorage().GetPremiums()),
        (EstimateTypes.LR, _) => GetStorage().GetCoverageUnits().Concat(GetStorage().GetNonAttributableAmountType()).Concat(AmountTypes.CDR.RepeatOnce()).Concat(GetStorage().GetAttributableExpenses()),
        (_, ValuationApproaches.PAA) => GetStorage().GetCoverageUnits().Concat(GetStorage().GetNonAttributableAmountType()).Concat(AmountTypes.CDR.RepeatOnce()).Concat(GetStorage().GetDeferrableExpenses()).Concat(GetStorage().GetPremiums()),
        (_) => GetStorage().GetCoverageUnits().Concat(GetStorage().GetNonAttributableAmountType()).Concat(AmountTypes.CDR.RepeatOnce())
    };

    IEnumerable<string> Values => GetScope<ValidAmountType>(Identity.Id.DataNode).BeAmountTypes.Except(amountTypesToExclude);
}


public interface TechnicalMargin : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) => 
        builder.ForScope<TechnicalMargin>(s => s.WithApplicability<TechnicalMarginForCurrentBasis>(x => x.Identity.ValuationApproach == ValuationApproaches.VFA, p => p.ForMember(s => s.EconomicBasis))
                                               .WithApplicability<TechnicalMarginForBopProjection>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && x.Identity.ProjectionPeriod > 0)
                                               .WithApplicability<TechnicalMarginForBOP>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I)
                                               .WithApplicability<TechnicalMarginDefaultValue>(x => x.Identity.AocType == AocTypes.CF)
                                               .WithApplicability<TechnicalMarginForIaStandard>(x => x.Identity.AocType == AocTypes.IA)// && x.Identity.Novelty == Novelties.I)
                                               //.WithApplicability<TechnicalMarginForIaNewBusiness>(x => x.Identity.AocType == AocTypes.IA)
                                               .WithApplicability<TechnicalMarginForEA>(x => x.Identity.AocType == AocTypes.EA && !x.Identity.IsReinsurance)
                                               .WithApplicability<TechnicalMarginForAM>(x => x.Identity.AocType == AocTypes.AM)
                                               .WithApplicability<TechnicalMarginForEop>(x => x.Identity.AocType == AocTypes.EOP)
                                               .WithApplicability<TechnicalMarginForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA)
                                               );
    
    protected string estimateType => GetContext();
    [NotVisible] string EconomicBasis => EconomicBases.L;
    double Value => GetScope<TechnicalMarginAmountType>((Identity, estimateType)).Values
                       .Sum(at => GetScope<PvAggregatedOverAccidentYear>((Identity, at, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value) +
                       GetScope<PvAggregatedOverAccidentYear>((Identity, (string)null, EstimateTypes.RA), o => o.WithContext(EconomicBasis)).Value;
                    
    double AggregatedValue => GetScope<PreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                .Sum(aoc => GetScope<TechnicalMargin>(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}).Value);
}

public interface TechnicalMarginForCurrentBasis : TechnicalMargin{
    [NotVisible] string TechnicalMargin.EconomicBasis => EconomicBases.C;
}

public interface TechnicalMarginForPaa : TechnicalMargin{
    [NotVisible] string TechnicalMargin.EconomicBasis => EconomicBases.L;
    double TechnicalMargin.Value => GetScope<TechnicalMarginAmountType>((Identity, estimateType)).Values
                       .Sum(at => GetScope<PvAggregatedOverAccidentYear>((Identity, at, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value) +
                       GetScope<PvAggregatedOverAccidentYear>((Identity, (string)null, EstimateTypes.RA), o => o.WithContext(EconomicBasis)).Value +
                       GetScope<DiscountedDeferrable>(Identity).Value + GetScope<PremiumRevenue>(Identity).Value;
}

public interface TechnicalMarginForBopProjection: TechnicalMargin{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) => 
        builder.ForScope<TechnicalMarginForBopProjection>(s => s
            .WithApplicability<TechnicalMarginAfterFirstYear>(x => x.Identity.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection));
    double TechnicalMargin.Value => GetScope<TechnicalMargin>(Identity with {ProjectionPeriod = 0}).Value;
}
public interface TechnicalMarginAfterFirstYear : TechnicalMargin{
    double TechnicalMargin.Value => GetScope<TechnicalMargin>(Identity with {AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1}).Value;
}

public interface TechnicalMarginForBOP : TechnicalMargin
{
    private double ValueCsm => GetStorage().GetValue(Identity, null, EstimateTypes.C, null, Identity.ProjectionPeriod);
    private double ValueLc => GetStorage().GetValue(Identity, null, EstimateTypes.L, null, Identity.ProjectionPeriod);
    
    double TechnicalMargin.Value => -1d * ValueCsm + ValueLc;
}

public interface TechnicalMarginDefaultValue : TechnicalMargin{
    double TechnicalMargin.Value => default;
}

public interface TechnicalMarginForIaStandard : TechnicalMargin, InterestAccretionFactor{
    double TechnicalMargin.Value => AggregatedValue * GetInterestAccretionFactor(EconomicBasis);
}

// public interface TechnicalMarginForIaNewBusiness : TechnicalMargin, NewBusinessInterestAccretion {
//     private int?[] accidentYears => GetStorage().GetAccidentYears(Identity.DataNode, Identity.ProjectionPeriod).ToArray();
//     private string[] amountTypes => GetScope<TechnicalMarginAmountType>((Identity, estimateType)).Values.ToArray();

//     private double[] nominalCashflows => accidentYears.SelectMany(ay =>
//         amountTypes.Select(at => GetScope<NominalCashflow>((Identity, at, EstimateTypes.BE, ay)).Values))
//         .AggregateDoubleArray()
//         .Concat(GetScope<NominalCashflow>((Identity, (string)null, EstimateTypes.RA, (int?)null)).Values)
//         .ToArray();
        
//     double TechnicalMargin.Value => GetStorage().ImportFormat != ImportFormats.Cashflow || GetStorage().IsSecondaryScope(Identity.DataNode) // This is normally an applicability for the scope, but this is the only case --> to be re-checked
//         ? (estimateType == EstimateTypes.LR) 
//             ? GetStorage().GetValue(Identity, null, estimateType, EconomicBasis, (int?)null, Identity.ProjectionPeriod)
//             : new [] {EstimateTypes.C, EstimateTypes.L}.Select(et => GetStorage().GetValue(Identity, null, et, EconomicBasis, (int?)null, Identity.ProjectionPeriod)).Sum()
//         : GetInterestAccretion(nominalCashflows, EconomicBasis);  
// }

public interface TechnicalMarginForEA : TechnicalMargin{
    static ApplicabilityBuilder ScopeApplicabilityBuilderInner(ApplicabilityBuilder builder) => 
        builder.ForScope<TechnicalMarginForEA>(s => s
            .WithApplicability<TechnicalMarginDefaultValue>(x => x.Identity.IsReinsurance)
            .WithApplicability<TechnicalMarginForEAForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA)
        );
    protected string referenceAocType => GetScope<ReferenceAocStep>(Identity).Values.First().AocType;// ReferenceAocStep of EA is CF
    protected double premiums => GetStorage().GetNovelties(referenceAocType, StructureType.AocPresentValue)
        .Sum(n => GetScope<BeExperienceAdjustmentForPremium>(Identity with {AocType = referenceAocType, Novelty = n}, o => o.WithContext(EconomicBasis)).Value) -
        GetScope<ActualExperienceAdjustmentOnPremium>(Identity with {AocType = referenceAocType, Novelty = Novelties.C}).Value;
    protected double deferrable => GetStorage().GetDeferrableExpenses().Sum(d =>
        GetStorage().GetNovelties(referenceAocType, StructureType.AocPresentValue).Sum(n => GetScope<PvAggregatedOverAccidentYear>((Identity with {AocType = referenceAocType, Novelty = n}, d, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value) -
        GetScope<WrittenActual>((Identity with {AocType = referenceAocType, Novelty = Novelties.C}, d, EstimateTypes.A, (int?)null)).Value);
    protected double investmentClaims => GetStorage().GetInvestmentClaims().Sum(ic =>
        GetStorage().GetNovelties(referenceAocType, StructureType.AocPresentValue).Sum(n => GetScope<PvAggregatedOverAccidentYear>((Identity with {AocType = referenceAocType, Novelty = n}, ic, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value) -
        GetScope<WrittenActual>((Identity with {AocType = referenceAocType, Novelty = Novelties.C}, ic, EstimateTypes.A, (int?)null)).Value);
    
    double TechnicalMargin.Value => premiums + deferrable + investmentClaims;
}

public interface TechnicalMarginForEAForPaa: TechnicalMarginForEA {
    double TechnicalMarginForEA.deferrable => GetScope<DiscountedDeferrable>(Identity with {AocType = AocTypes.AM, Novelty = Novelties.C}).Value -
        GetStorage().GetDeferrableExpenses().Sum(d => GetScope<WrittenActual>((Identity with {AocType = referenceAocType, Novelty = Novelties.C}, d, EstimateTypes.A, (int?)null)).Value);
}

public interface TechnicalMarginForAM : TechnicalMargin{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<TechnicalMarginForAM>(s => s.WithApplicability<TechnicalMarginForAmForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA));   
 
    double TechnicalMargin.Value => Math.Abs(AggregatedValue) > Consts.Precision ? -1d * AggregatedValue * GetScope<CurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).Value : default;
}

public interface TechnicalMarginForAmForPaa : TechnicalMargin{
    private IEnumerable<string> novelties => GetStorage().GetNovelties(AocTypes.CF, StructureType.AocPresentValue);
    double TechnicalMargin.Value =>  GetScope<TechnicalMarginAmountType>((Identity, estimateType)).Values
                                   .Sum(at => novelties.Sum(n => GetScope<PvAggregatedOverAccidentYear>((Identity with {AocType = AocTypes.CF, Novelty = n}, at, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value + 
                                                                 GetScope<PvAggregatedOverAccidentYear>((Identity with {AocType = AocTypes.CF, Novelty = n}, (string)null, EstimateTypes.RA), o => o.WithContext(EconomicBasis)).Value));
//+  Revenue AM + Deferral AM

}

public interface TechnicalMarginForEop : TechnicalMargin{
    double TechnicalMargin.Value  => GetScope<PreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                                .Sum(aoc => GetScope<TechnicalMargin>(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}).Value);
}


public interface AllocateTechnicalMarginWithIfrsVariable: IScope<ImportIdentity, ImportStorage>
{                                  
    double Value => ComputeTechnicalMarginFromIfrsVariables(Identity);
    double AggregatedValue => GetScope<PreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                               .Sum(aoc => ComputeTechnicalMarginFromIfrsVariables(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}));
                                                                    
    private double ComputeTechnicalMarginFromIfrsVariables(ImportIdentity id) =>
        GetStorage().GetValue(Identity, null, EstimateTypes.L, null, Identity.ProjectionPeriod) - 
        GetStorage().GetValue(Identity, null, EstimateTypes.C, null, Identity.ProjectionPeriod);
}


public interface AllocateTechnicalMargin: IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) => 
        builder.ForScope<AllocateTechnicalMargin>(s => s
                                             .WithApplicability<ComputeAllocateTechnicalMarginWithIfrsVariable>(x => x.GetStorage().IsSecondaryScope(x.Identity.DataNode))
                                             .WithApplicability<AllocateTechnicalMarginForBop>(x => x.Identity.AocType == AocTypes.BOP)
                                             .WithApplicability<AllocateTechnicalMarginForCl>(x => x.Identity.AocType == AocTypes.CL)
                                             .WithApplicability<AllocateTechnicalMarginForEop>(x => x.Identity.AocType == AocTypes.EOP)
                                             );
    
    [NotVisible] double AggregatedTechnicalMargin => GetScope<TechnicalMargin>(Identity).AggregatedValue; 
    [NotVisible] double TechnicalMargin => GetScope<TechnicalMargin>(Identity).Value;
    [NotVisible] string ComputedEstimateType => ComputeEstimateType(GetScope<TechnicalMargin>(Identity).AggregatedValue + TechnicalMargin);
    [NotVisible] bool HasSwitch => ComputedEstimateType != ComputeEstimateType(GetScope<TechnicalMargin>(Identity).AggregatedValue);
     
    [NotVisible] string EstimateType => GetContext();
    
    double Value => (HasSwitch, EstimateType == ComputedEstimateType) switch {
            (true, true) => TechnicalMargin + AggregatedTechnicalMargin,
            (true, false) => -1d * AggregatedTechnicalMargin,
            (false, true) => TechnicalMargin,
            _ => default
        };
    
    string ComputeEstimateType(double aggregatedTechnicalMargin) => aggregatedTechnicalMargin > Consts.Precision ? EstimateTypes.L : EstimateTypes.C;
}

public interface ComputeAllocateTechnicalMarginWithIfrsVariable : AllocateTechnicalMargin
{                                  
    double AllocateTechnicalMargin.TechnicalMargin => GetScope<AllocateTechnicalMarginWithIfrsVariable>(Identity).Value;
    double AllocateTechnicalMargin.AggregatedTechnicalMargin => GetScope<AllocateTechnicalMarginWithIfrsVariable>(Identity).AggregatedValue;
}

public interface AllocateTechnicalMarginForCl : AllocateTechnicalMargin
{
    private double balancingValue => GetScope<PreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                        .GroupBy(x => x.Novelty, (k, v) => v.Last())
                                        .Sum(aoc => { 
                                            var id = Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty};
                                            return GetScope<AllocateTechnicalMargin>(id).ComputedEstimateType != ComputedEstimateType ? 
                                                   GetScope<AllocateTechnicalMargin>(id).TechnicalMargin + GetScope<AllocateTechnicalMargin>(id).AggregatedTechnicalMargin
                                                   : (double)default; });

    [NotVisible] bool AllocateTechnicalMargin.HasSwitch => Math.Abs(balancingValue) > Consts.Precision;
    [NotVisible] double AllocateTechnicalMargin.AggregatedTechnicalMargin => balancingValue;
}

public interface AllocateTechnicalMarginForBop : AllocateTechnicalMargin {
    bool AllocateTechnicalMargin.HasSwitch => false;
}

public interface AllocateTechnicalMarginForEop : AllocateTechnicalMargin
{
    double AllocateTechnicalMargin.Value  => GetScope<PreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                                .Sum(aoc => GetScope<AllocateTechnicalMargin>(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}).Value);
    [NotVisible] string AllocateTechnicalMargin.ComputedEstimateType => ComputeEstimateType(AggregatedTechnicalMargin);
}

public interface AllocateTechnicalMarginForBopProjection: AllocateTechnicalMargin{
    double AllocateTechnicalMargin.TechnicalMargin => GetScope<AllocateTechnicalMargin>(Identity with {AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1}).Value;
    bool AllocateTechnicalMargin.HasSwitch => false;
}


public interface ContractualServiceMargin : IScope<ImportIdentity, ImportStorage>
{
    [NotVisible]string EstimateType => EstimateTypes.C;
     
    double Value => Identity.IsReinsurance 
        ? -1d * GetScope<TechnicalMargin>(Identity, o => o.WithContext(EstimateType)).Value
        : -1d * GetScope<AllocateTechnicalMargin>(Identity, o => o.WithContext(EstimateType)).Value;
}


public interface LossComponent : IScope<ImportIdentity, ImportStorage>
{
    [NotVisible]string EstimateType => EstimateTypes.L;
    
    double Value => GetScope<AllocateTechnicalMargin>(Identity, o => o.WithContext(EstimateType)).Value;
}


public interface LoReCoBoundary : IScope<ImportIdentity, ImportStorage>
{
    private IEnumerable<string> underlyingGic => GetStorage().GetUnderlyingGic(Identity, LiabilityTypes.LRC);
   
    double Value => underlyingGic.Sum(gic => GetStorage().GetReinsuranceCoverage(Identity, gic) * GetScope<LossComponent>(GetStorage().GetUnderlyingIdentity(Identity, gic)).Value);
                                                                      
    double AggregatedValue => underlyingGic.Sum(gic => GetStorage().GetReinsuranceCoverage(Identity, gic) * 
                                                       GetScope<PreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                                            .Sum(aoc => GetScope<LossComponent>(Identity with {DataNode = gic, AocType = aoc.AocType, Novelty = aoc.Novelty}).Value));
}


public interface LossRecoveryComponent : IScope<ImportIdentity, ImportStorage>
{    
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) => 
        builder.ForScope<LossRecoveryComponent>(s => s
                                             .WithApplicability<LossRecoveryComponentForBopProjection>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && x.Identity.ProjectionPeriod > 0)
                                             .WithApplicability<LossRecoveryComponentForBop>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && !x.GetStorage().IsInceptionYear(x.Identity.DataNode))
                                             .WithApplicability<LossRecoveryComponentPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA || x.Identity.AocType == AocTypes.BOP)
                                             .WithApplicability<LossRecoveryComponentForAm>(x => x.Identity.AocType == AocTypes.AM)
                                             .WithApplicability<LossRecoveryComponentForEop>(x => x.Identity.AocType == AocTypes.EOP)
                                            );

    protected double loReCoBoundaryValue => GetScope<LoReCoBoundary>(Identity).Value;
    private double aggregatedLoReCoBoundary => GetScope<LoReCoBoundary>(Identity).AggregatedValue;
    private double reinsuranceCsm => GetScope<TechnicalMargin>(Identity, o => o.WithContext(EstimateType)).Value;
    private double aggregatedLoReCoProjectionWithFm => AggregatedValue + reinsuranceCsm;   
    private bool isAboveUpperBoundary => aggregatedLoReCoProjectionWithFm >= Consts.Precision;
    private bool isBelowLowerBoundary => aggregatedLoReCoProjectionWithFm < -1d * (aggregatedLoReCoBoundary + loReCoBoundaryValue);
    private double marginToLowerBoundary => -1d * (AggregatedValue + aggregatedLoReCoBoundary + loReCoBoundaryValue);
    
    protected double AggregatedValue => GetScope<PreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
            .Sum(aoc => GetScope<LossRecoveryComponent>(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}).Value);

    double Value => (isAboveUpperBoundary, isBelowLowerBoundary) switch {
        (false , false) => reinsuranceCsm,
        (false , true) => marginToLowerBoundary,
        (true  , false) => -1d * AggregatedValue,
        _ => default
    };

    [NotVisible]string EstimateType => EstimateTypes.LR;
}

public interface LossRecoveryComponentForBopProjection: LossRecoveryComponent{
    double LossRecoveryComponent.Value => GetScope<LossRecoveryComponent>(Identity with {AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1}).Value;
}

public interface LossRecoveryComponentForBop : LossRecoveryComponent{
     double LossRecoveryComponent.Value => -1d * GetStorage().GetValue(Identity, null, EstimateTypes.LR, null, Identity.ProjectionPeriod);
 }

public interface LossRecoveryComponentPaa : LossRecoveryComponent{
    double LossRecoveryComponent.Value => -1d * loReCoBoundaryValue;
}

public interface LossRecoveryComponentForAm : LossRecoveryComponent{
    private string economicBasis => GetScope<TechnicalMargin>(Identity).EconomicBasis;
    double LossRecoveryComponent.Value  => Math.Abs(AggregatedValue) > Consts.Precision ? -1d * AggregatedValue * GetScope<CurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(economicBasis)).Value : default;
}

public interface LossRecoveryComponentForEop : LossRecoveryComponent{
    double LossRecoveryComponent.Value  => GetScope<PreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                                .Sum(aoc => GetScope<LossRecoveryComponent>(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}).Value);
}
