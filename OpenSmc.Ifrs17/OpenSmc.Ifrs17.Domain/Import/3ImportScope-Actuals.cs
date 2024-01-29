using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;
using OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface WrittenActual : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<WrittenActual>(s => s
            .WithApplicability<ActualEmptyValue>(x => !(x.Identity.Id.AocType == AocTypes.CF && x.Identity.Id.Novelty == Novelties.C))
            .WithApplicability<ActualProjection>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.ProjectionPeriod > 0)
            .WithApplicability<ActualFromPaymentPattern>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && Math.Abs(x.Value) < Consts.Precision));

    double Value => GetStorage().GetValue(Identity.Id, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear, Identity.Id.ProjectionPeriod);
}

public interface ActualEmptyValue : WrittenActual{
    double WrittenActual.Value => default;
}

public interface ActualProjection : WrittenActual{
    double WrittenActual.Value => GetStorage().GetValues(Identity.Id with {AocType = AocTypes.CL, Novelty = Novelties.C}, Identity.AmountType, EstimateTypes.PCE, Identity.AccidentYear).Any()
        ? GetScope<ActualFromPaymentPattern>(Identity).Value
        : GetStorage().GetNovelties(Identity.Id.AocType, StructureType.AocPresentValue)
            .Sum(novelty => GetScope<IPresentValue>((Identity.Id with {AocType = AocTypes.CF, Novelty = novelty}, Identity.AmountType, EstimateTypes.BE, Identity.AccidentYear), o => o.WithContext(EconomicBases.C)).Value);
}

public interface ActualFromPaymentPattern : WrittenActual, IWithGetValueFromValues{
    double WrittenActual.Value => GetValueFromValues(
        GetStorage().GetValues(Identity.Id with {AocType = AocTypes.CL, Novelty = Novelties.C}, Identity.AmountType, EstimateTypes.PCE, Identity.AccidentYear),
        ValuationPeriod.Delta.ToString());
}


public interface Actual : IScope<ImportIdentity, ImportStorage>{
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.A;
       
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] Actuals => 
        GetScope<IValidAmountType>(Identity.DataNode).AllImportedAmountTypes
            .SelectMany(amountType => GetStorage().GetAccidentYears(Identity.DataNode, Identity.ProjectionPeriod)
                .Select(accidentYear => (amountType, EstimateType, accidentYear, GetScope<WrittenActual>((Identity, amountType, EstimateType, accidentYear)).Value ))).ToArray();
}



public interface AccrualActual : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<AccrualActual>(s => s
            .WithApplicability<AccrualEmptyValues>(x => !x.GetStorage().GetAllAocSteps(StructureType.AocAccrual).Contains(x.Identity.Id.AocStep))
            .WithApplicability<AccrualEndOfPeriod>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.AocType == AocTypes.EOP)
            .WithApplicability<AccrualEmptyValues>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.ProjectionPeriod > x.GetStorage().FirstNextYearProjection) // projections beyond ReportingYear +1
            .WithApplicability<AccrualProjectionFirstYear>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.ProjectionPeriod == x.GetStorage().FirstNextYearProjection && // projections ReportingYear +1
                                                                (x.Identity.Id.AocType == AocTypes.BOP || x.Identity.Id.AocType == AocTypes.CF))
            .WithApplicability<AccrualEmptyValues>(x => x.Identity.Id.ProjectionPeriod == x.GetStorage().FirstNextYearProjection && x.Identity.Id.AocType == AocTypes.WO)                                                                
            .WithApplicability<AccrualProjectionWithinFirstYear>(x => !x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode) && x.Identity.Id.ProjectionPeriod > 0)
            );

    public double Value => GetStorage().GetValue(Identity.Id, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear, Identity.Id.ProjectionPeriod); 
}

public interface AccrualProjectionWithinFirstYear : AccrualActual{
    double AccrualActual.Value => GetScope<AccrualActual>((Identity.Id with {ProjectionPeriod = 0}, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear )).Value;
}

public interface AccrualProjectionFirstYear : AccrualActual{
    private double signMultiplier => Identity.Id.AocType == AocTypes.BOP ? 1d : -1d;
    double AccrualActual.Value => signMultiplier * GetScope<AccrualActual>((Identity.Id with {AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.Id.ProjectionPeriod - 1}, 
        Identity.AmountType, Identity.EstimateType, Identity.AccidentYear)).Value;
}

public interface AccrualEndOfPeriod : AccrualActual{
    double AccrualActual.Value => GetScope<IPreviousAocSteps>((Identity.Id, StructureType.AocAccrual)).Values.Sum(aocStep => 
        GetScope<AccrualActual>((Identity.Id with {AocType = aocStep.AocType, Novelty = aocStep.Novelty}, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear)).Value);
}

public interface AccrualEmptyValues : AccrualActual{
    double AccrualActual.Value => default;
}


public interface AdvanceActual : IScope<ImportIdentity, ImportStorage>
{
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.AA;
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] Actuals => 
        GetScope<IValidAmountType>(Identity.DataNode).ActualAmountTypes
            .SelectMany(amountType => GetStorage().GetAccidentYears(Identity.DataNode, Identity.ProjectionPeriod)
                .Select(accidentYear => (amountType, EstimateType, accidentYear, GetScope<AccrualActual>((Identity, amountType, EstimateType, accidentYear)).Value) )).ToArray();
}


public interface OverdueActual : IScope<ImportIdentity, ImportStorage>
{
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.OA;

    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] Actuals => 
        GetScope<IValidAmountType>(Identity.DataNode).ActualAmountTypes
            .SelectMany(amountType => GetStorage().GetAccidentYears(Identity.DataNode, Identity.ProjectionPeriod)
                .Select(accidentYear => (amountType, EstimateType, accidentYear, GetScope<AccrualActual>((Identity, amountType, EstimateType, accidentYear)).Value) )).ToArray();
}


public interface DiscountedAmortizationFactorForDeferrals : IScope<ImportIdentity, ImportStorage>
{
    private string EconomicBasis => GetContext();
    double Value => GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.DAE, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType == AmountTypes.DAE
        ? GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.DAE, 0), o => o.WithContext(EconomicBasis)).Value
        : GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).Value;
    string? EffectiveAmountType => GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.DAE, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType == AmountTypes.DAE
        ? AmountTypes.DAE
        : GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType;
}


public interface DiscountedDeferrable : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<DiscountedDeferrable>(s => s
            .WithApplicability<DeferrableWithIfrsVariable>(x => x.GetStorage().IsSecondaryScope(x.Identity.DataNode))
            .WithApplicability<DeferrableForBopProjection>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && x.Identity.ProjectionPeriod > 0)
            .WithApplicability<DeferrableForBop>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I)
            .WithApplicability<DeferrableForIaStandard>(x => x.Identity.AocType == AocTypes.IA) // && x.Identity.Novelty == Novelties.I)
            //WithApplicability<DeferrableForIaNewBusiness>(x => x.Identity.AocType == AocTypes.IA)
            .WithApplicability<DeferrableDefaultValue>(x => x.Identity.AocType == AocTypes.CF)
            .WithApplicability<DeferrableEa>(x => x.Identity.AocType == AocTypes.EA)
            .WithApplicability<DeferrableAm>(x => x.Identity.AocType == AocTypes.AM)
            .WithApplicability<DeferrableEop>(x => x.Identity.AocType == AocTypes.EOP)
        );
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => EstimateTypes.DA;
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string AmountType => null;
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);
    
    double Value => GetStorage().GetDeferrableExpenses().Sum(at => 
        GetScope<IPresentValue>((Identity, at, EstimateTypes.BE, (int?)null), o => o.WithContext(EconomicBasis)).Value);
}

public interface DeferrableWithIfrsVariable : DiscountedDeferrable {
    double DiscountedDeferrable.Value => GetStorage().GetValue(Identity, AmountType, EstimateType, EconomicBasis, (int?)null, Identity.ProjectionPeriod);
}

public interface DeferrableForBopProjection : DiscountedDeferrable {
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<DiscountedDeferrable>(s => s
            .WithApplicability<DeferrableAfterFirstYear>(x => x.Identity.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection));
    double DiscountedDeferrable.Value => GetScope<DiscountedDeferrable>(Identity with {ProjectionPeriod = 0}).Value;
}

public interface DeferrableAfterFirstYear : DiscountedDeferrable {
    double DiscountedDeferrable.Value => GetScope<DiscountedDeferrable>(Identity with {AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1}).Value;
}   

public interface DeferrableForBop : DiscountedDeferrable {
    //loop over amountTypes within deferrals to get bops
    double DiscountedDeferrable.Value => GetStorage().GetValue(Identity, null, EstimateTypes.DA, (int?)null, Identity.ProjectionPeriod);
}

public interface DeferrableForIaStandard : DiscountedDeferrable, IInterestAccretionFactor {
    private double aggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => GetScope<DiscountedDeferrable>(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}).Value);   
    double DiscountedDeferrable.Value => aggregatedValue * GetInterestAccretionFactor(EconomicBasis);
}

// public interface DeferrableForIaNewBusiness : DiscountedDeferrable, NewBusinessInterestAccretion {
//     private double[] nominalCashflows => GetStorage().GetDeferrableExpenses().Select(at => 
//         GetScope<NominalCashflow>((Identity, at, EstimateTypes.BE, (int?)null)).Values).AggregateDoubleArray();

//     double DiscountedDeferrable.Value => GetStorage().ImportFormat != ImportFormats.Cashflow || GetStorage().IsSecondaryScope(Identity.DataNode) // This is normally an applicability for the scope, but this is the only case --> to be re-checked
//         ? GetStorage().GetValue(Identity, null, EstimateType, EconomicBasis, (int?)null, Identity.ProjectionPeriod)
//         : -1d * GetInterestAccretion(nominalCashflows, EconomicBasis);
// }

public interface DeferrableDefaultValue : DiscountedDeferrable {
    double DiscountedDeferrable.Value => default;
}

public interface DeferrableEa : DiscountedDeferrable {
    private string referenceAocType => GetScope<IReferenceAocStep>(Identity).Values.First().AocType;
    double DiscountedDeferrable.Value => GetStorage().GetDeferrableExpenses().Sum(at =>
        GetStorage().GetNovelties(referenceAocType, StructureType.AocPresentValue)
        .Sum(n => GetScope<IPresentValue>((Identity with {AocType = referenceAocType, Novelty = n}, at, EstimateTypes.BE, (int?)null), o => o.WithContext(EconomicBasis)).Value) -
             GetScope<WrittenActual>((Identity with {AocType = referenceAocType, Novelty = Novelties.C}, at, EstimateTypes.A, (int?)null)).Value);
}

public interface DeferrableAm : DiscountedDeferrable {
    private double amortizationFactor => GetScope<DiscountedAmortizationFactorForDeferrals>(Identity, o => o.WithContext(EconomicBasis)).Value;
    private double aggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                            .Sum(aocStep => GetScope<DiscountedDeferrable>(Identity with {AocType = aocStep.AocType, Novelty = aocStep.Novelty}).Value);
    double DiscountedDeferrable.Value => Math.Abs(aggregatedValue) > Consts.Precision ? -1d * aggregatedValue * amortizationFactor : default;
}

public interface DeferrableEop : DiscountedDeferrable {
    double DiscountedDeferrable.Value => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                        .Sum(aocStep => GetScope<DiscountedDeferrable>(Identity with {AocType = aocStep.AocType, Novelty = aocStep.Novelty}).Value);
}


//TODO : 
// EstimateType from DA to DAC
// BOP,I only through Opening. 
public interface NominalDeferrable : IScope<(ImportIdentity Id, int MonthlyShift), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
            builder.ForScope<NominalDeferrable>(s => s
                .WithApplicability<NominalDeferrableWithIfrsVariable>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow || x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode))
                .WithApplicability<BoPDeferrableProjection>(x => x.Identity.Id.AocType == AocTypes.BOP && x.Identity.Id.Novelty == Novelties.I && x.Identity.Id.ProjectionPeriod > 0)
                .WithApplicability<BoPDeferrable>(x => x.Identity.Id.AocType == AocTypes.BOP)
                .WithApplicability<AmDeferrable>(x => x.Identity.Id.AocType == AocTypes.AM)
                .WithApplicability<EopDeferrable>(x => x.Identity.Id.AocType == AocTypes.EOP)
            );
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => EstimateTypes.DA;
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string AmountType => null;
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] string EconomicBasis => EconomicBases.N;
    double Value => default;
}

public interface NominalDeferrableWithIfrsVariable : NominalDeferrable {
    double NominalDeferrable.Value => GetStorage().GetValue(Identity.Id, AmountType, EstimateType, EconomicBasis, Identity.MonthlyShift, Identity.Id.ProjectionPeriod);
}

public interface BoPDeferrableProjection : NominalDeferrable{
    double NominalDeferrable.Value => GetScope<NominalDeferrable>((Identity.Id with {AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.Id.ProjectionPeriod - 1}, Identity.MonthlyShift)).Value;
}

public interface BoPDeferrable : NominalDeferrable{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
            builder.ForScope<NominalDeferrable>(s => s.WithApplicability<NominalDeferrableFromIfrsVariable>(x => x.Identity.Id.Novelty == Novelties.I));
    private int projectionShift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    double NominalDeferrable.Value => GetScope<INominalCashflow>((Identity.Id, AmountTypes.DAE, EstimateTypes.BE, (int?)null)).Values //loop over AM under DE
        .Skip(projectionShift + Identity.MonthlyShift).FirstOrDefault();
}

public interface NominalDeferrableFromIfrsVariable : NominalDeferrable{
    double NominalDeferrable.Value => GetStorage().GetValue(Identity.Id, AmountType, EstimateTypes.DA, EconomicBasis, Identity.MonthlyShift, Identity.Id.ProjectionPeriod);
}

public interface AmReferenceDeferrable: IScope<(ImportIdentity Id, int MonthlyShift), ImportStorage>{
    private int projectionShift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    private IEnumerable<AocStep> previousAocSteps => GetScope<IPreviousAocSteps>((Identity.Id, StructureType.AocTechnicalMargin)).Values.Where(aocStep => aocStep.Novelty != Novelties.C);
    double referenceCashflow => previousAocSteps
        .GroupBy(x => x.Novelty, (k, aocs) => aocs.Last())
        .Sum(aoc => GetScope<INominalCashflow>((Identity.Id with {AocType = aoc.AocType, Novelty = aoc.Novelty}, AmountTypes.DAE, EstimateTypes.BE, (int?)null)).Values
        .Skip(projectionShift + Identity.MonthlyShift).FirstOrDefault());
    //if no previous RawVariable, use IfrsVariable
    double Value => Math.Abs(referenceCashflow) >= Consts.Precision ? referenceCashflow : GetStorage().GetNovelties(AocTypes.BOP, StructureType.AocPresentValue).Sum(n => GetScope<NominalDeferrable>((Identity.Id with {AocType = AocTypes.BOP, Novelty = n}, Identity.MonthlyShift)).Value);
}

public interface AmDeferrable : NominalDeferrable{
    private IEnumerable<AocStep> referenceAocSteps => GetScope<IReferenceAocStep>(Identity.Id).Values; //Reference step of AM,C is CL,C
    private double referenceCashflow => referenceAocSteps.Sum(refAocStep => GetScope<AmReferenceDeferrable>((Identity.Id with {AocType = refAocStep.AocType, Novelty = refAocStep.Novelty}, Identity.MonthlyShift)).Value);

    double NominalDeferrable.Value => Math.Abs(referenceCashflow) > Consts.Precision ? -1d * referenceCashflow * GetScope<ICurrentPeriodAmortizationFactor>((Identity.Id, AmountTypes.DAE, Identity.MonthlyShift), o => o.WithContext(EconomicBasis)).Value : default;
}

public interface EopDeferrable : NominalDeferrable{
    private IEnumerable<AocStep> previousAocSteps => GetScope<IPreviousAocSteps>((Identity.Id, StructureType.AocTechnicalMargin)).Values;
    double NominalDeferrable.Value => previousAocSteps.Sum(aocStep => GetScope<NominalDeferrable>((Identity.Id with {AocType = aocStep.AocType, Novelty = aocStep.Novelty}, Identity.MonthlyShift)).Value);
}


public interface DiscountedAmortizationFactorForRevenues : IScope<ImportIdentity, ImportStorage>
{
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] private string EconomicBasis => GetContext();
    
    double Value => GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.PR, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType == AmountTypes.PR
        ? GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.PR, 0), o => o.WithContext(EconomicBasis)).Value
        : GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).Value;

    string? EffectiveAmountType => GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.PR, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType == AmountTypes.PR
        ? AmountTypes.PR
        : GetScope<ICurrentPeriodAmortizationFactor>((Identity, AmountTypes.CU, 0), o => o.WithContext(EconomicBasis)).EffectiveAmountType;
}


//only PAA LRC
public interface PremiumRevenue : IScope<ImportIdentity, ImportStorage>{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<PremiumRevenue>(s => s
            .WithApplicability<PremiumRevenueWithIfrsVariable>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow || x.GetStorage().IsSecondaryScope(x.Identity.DataNode))
            .WithApplicability<PremiumRevenueForBopProjection>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && x.Identity.ProjectionPeriod > 0)
            .WithApplicability<PremiumRevenueForBop>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I)
            .WithApplicability<PremiumRevenueForIaStandard>(x => x.Identity.AocType == AocTypes.IA) // && x.Identity.Novelty == Novelties.I)
            //.WithApplicability<PremiumRevenueForIaNewBusiness>(x => x.Identity.AocType == AocTypes.IA)
            .WithApplicability<PremiumRevenueDefaultValue>(x => new []{AocTypes.CF, AocTypes.EA}.Contains(x.Identity.AocType))
            //TODO compute EA but in the case of no LC EA is 0
            .WithApplicability<PremiumRevenueAm>(x => x.Identity.AocType == AocTypes.AM)
            .WithApplicability<PremiumRevenueEop>(x => x.Identity.AocType == AocTypes.EOP)
        );
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))] string EstimateType => EstimateTypes.R;
    [IdentityProperty][NotVisible][Dimension(typeof(AmountType))] string AmountType => null;
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))] string EconomicBasis => GetStorage().GetEconomicBasisDriver(Identity.DataNode);
    
    double Value => GetStorage().GetPremiums().Sum(at => 
        GetScope<IPresentValue>((Identity, at, EstimateTypes.BE, (int?)null), o => o.WithContext(EconomicBasis)).Value);
}

public interface PremiumRevenueWithIfrsVariable : PremiumRevenue {
    double PremiumRevenue.Value => GetStorage().GetValue(Identity, AmountType, EstimateType, EconomicBasis, (int?)null, Identity.ProjectionPeriod);
}

public interface PremiumRevenueForBopProjection : PremiumRevenue {
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<PremiumRevenueForBopProjection>(s => s
            .WithApplicability<PremiumRevenueAfterFirstYear>(x => x.Identity.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection));
    double PremiumRevenue.Value => GetScope<PremiumRevenue>(Identity with {ProjectionPeriod = 0}).Value;
}

public interface PremiumRevenueAfterFirstYear : PremiumRevenue {
    double PremiumRevenue.Value => GetScope<PremiumRevenue>(Identity with {AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1}).Value;
}

public interface PremiumRevenueForBop : PremiumRevenue {
    double PremiumRevenue.Value => GetStorage().GetValue(Identity, AmountType, EstimateTypes.R, EconomicBasis, (int?)null, Identity.ProjectionPeriod);
}

public interface AggregatedPremiumRevenue : PremiumRevenue {
    double AggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => GetScope<PremiumRevenue>(Identity with {AocType = aoc.AocType, Novelty = aoc.Novelty}).Value);
}

public interface PremiumRevenueForIaStandard : PremiumRevenue, IInterestAccretionFactor {
    private double aggregatedValue => GetScope<AggregatedPremiumRevenue>(Identity).AggregatedValue;   
    double PremiumRevenue.Value => aggregatedValue * GetInterestAccretionFactor(EconomicBasis);
}

// public interface PremiumRevenueForIaNewBusiness : PremiumRevenue, NewBusinessInterestAccretion {
//     private double[] nominalCashflows => GetStorage().GetPremiums().Select(at => 
//         GetScope<NominalCashflow>((Identity, at, EstimateTypes.BE, (int?)null)).Values).AggregateDoubleArray();

//     double PremiumRevenue.Value => GetStorage().ImportFormat != ImportFormats.Cashflow || GetStorage().IsSecondaryScope(Identity.DataNode) // This is normally an applicability for the scope, but this is the only case --> to be re-checked
//         ? GetStorage().GetValue(Identity, null, EstimateType, EconomicBasis, (int?)null, Identity.ProjectionPeriod)
//         : -1d * GetInterestAccretion(nominalCashflows, EconomicBasis);
// }

public interface PremiumRevenueDefaultValue : PremiumRevenue {
    double PremiumRevenue.Value => default;
}

public interface PremiumRevenueAm : PremiumRevenue {
    private double AmortizationFactor => GetScope<DiscountedAmortizationFactorForRevenues>(Identity, o => o.WithContext(EconomicBasis)).Value;
    private double aggregatedValue => GetScope<AggregatedPremiumRevenue>(Identity).AggregatedValue;
    double PremiumRevenue.Value => Math.Abs(aggregatedValue) > Consts.Precision ? -1d * aggregatedValue * AmortizationFactor : default;
}

public interface PremiumRevenueEop : PremiumRevenue {
    double PremiumRevenue.Value => GetScope<AggregatedPremiumRevenue>(Identity).AggregatedValue;
}



