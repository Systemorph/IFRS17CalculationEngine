//#!import "1ImportScope-Identities"


using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;
using Systemorph.Arithmetics;
using Systemorph.Vertex.Arithmetics;

public interface MonthlyRate : IScope<ImportIdentity, ImportStorage>
{
    private string EconomicBasis => GetContext();
    
    private double[] YearlyYieldCurve => EconomicBasis switch {
        EconomicBases.N => new [] { 0d },
        _ => GetStorage().GetYearlyYieldCurve(Identity, EconomicBasis),
    };
    
    double[] Interest => YearlyYieldCurve.Select(rate => Math.Pow(1d + rate, 1d / 12d)).ToArray();   
                        
    double[] Discount => Interest.Select(x => Math.Pow(x, -1)).ToArray();
}


public interface NominalCashflow : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<NominalCashflow>(s => s
            .WithApplicability<EmptyNominalCashflow>(x =>
                (x.Identity.Id.AocType != AocTypes.CL && x.Identity.Id.AocType != AocTypes.EOP) && // if AocType is NOT CL AND NOT EOP AND
                x.Identity.Id.Novelty != Novelties.I && // if Novelty is NOT inforce AND
                x.Identity.Id.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection && // if it is projection >= 1 Year AND
                !(x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if it DOES NOT (have AY and with AY >= than projected FY)
            )
            .WithApplicability<EmptyNominalCashflow>(x =>
                (x.Identity.Id.AocType == AocTypes.BOP || x.Identity.Id.AocType == AocTypes.CF || x.Identity.Id.AocType == AocTypes.IA) && // if AocType is BOP, CF or IA (or not in telescopic) AND
                x.Identity.Id.Novelty == Novelties.I && // if Novelty is inforce AND
                x.Identity.Id.LiabilityType == LiabilityTypes.LIC && // if LiabilityType is LIC AND
                x.Identity.Id.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection && // if it is projection >= 1 Year AND
                (x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if it DOES (have AY and with AY >= than projected FY)
            )
            .WithApplicability<EmptyNominalCashflow>(x =>
                x.Identity.Id.LiabilityType == LiabilityTypes.LRC && // if LiabilityType is LRC
                x.Identity.Id.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection && // if it is projection >= 1 Year AND
                (x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear < (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if it DOES (have AY and with AY < than projected FY)
            )
            .WithApplicability<EmptyNominalCashflow>(x =>
                (x.Identity.Id.AocType == AocTypes.BOP || x.Identity.Id.AocType == AocTypes.CF || x.Identity.Id.AocType == AocTypes.IA) && // if AocType is BOP, CF or IA (or not in telescopic) AND
                (x.Identity.Id.Novelty != Novelties.I && x.Identity.Id.Novelty != Novelties.C) && // if Novelty is NOT inforce AND
                x.Identity.Id.LiabilityType == LiabilityTypes.LRC && // if LiabilityType is LRC AND
                x.Identity.Id.ProjectionPeriod >= x.GetStorage().FirstNextYearProjection && // if it is projection >= 1 Year AND
                (x.Identity.AccidentYear.HasValue && Consts.MonthInAYear * x.Identity.AccidentYear >= (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if it DOES (have AY and with AY >= than projected FY)
            )
            .WithApplicability<EmptyNominalCashflow>(x =>
                (x.Identity.Id.AocType == AocTypes.CF) && // if AocType is CF AND
                x.Identity.Id.LiabilityType == LiabilityTypes.LRC && x.Identity.AccidentYear.HasValue && // if LiabilityType is LRC with AY defined
                x.Identity.Id.ProjectionPeriod < x.GetStorage().FirstNextYearProjection && //  if it is projection == 0 AND
                //x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod) == 0 && // if it is projection == 0 AND
                !(Consts.MonthInAYear * x.Identity.AccidentYear == (Consts.MonthInAYear * x.GetStorage().CurrentReportingPeriod.Year + x.GetStorage().GetShift(x.Identity.Id.ProjectionPeriod))) // if AY == projected FY
            )
            .WithApplicability<CreditDefaultRiskNominalCashflow>(x => x.GetStorage().GetCdr().Contains(x.Identity.AmountType) && x.Identity.Id.AocType == AocTypes.CF)
            .WithApplicability<AllClaimsCashflow>(x => x.GetStorage().GetCdr().Contains(x.Identity.AmountType))
        );

    IEnumerable<AocStep> referenceAocSteps => GetScope<ReferenceAocStep>(Identity.Id).Values;
    double[] Values => referenceAocSteps.Select(refAocStep => GetStorage().GetValues(Identity.Id with {AocType = refAocStep.AocType, Novelty = refAocStep.Novelty}, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear))
                        .AggregateDoubleArray();
}

public interface EmptyNominalCashflow : NominalCashflow
{
    double[] NominalCashflow.Values => Enumerable.Empty<double>().ToArray();
}

public interface CreditDefaultRiskNominalCashflow : NominalCashflow
{
    private double[] NominalClaimsCashflow => referenceAocSteps.SelectMany(refAocStep =>
                            GetStorage().GetClaims()
                            .Select(claim => GetStorage().GetValues(Identity.Id with {AocType = refAocStep.AocType, Novelty = refAocStep.Novelty}, claim, Identity.EstimateType, Identity.AccidentYear)))
                            .AggregateDoubleArray();
                            
    private string cdrBasis => Identity.AmountType == AmountTypes.CDR ? EconomicBases.C : EconomicBases.L;
    private double nonPerformanceRiskRate => GetStorage().GetNonPerformanceRiskRate(Identity.Id, cdrBasis);
                            
    private double[] PvCdrDecumulated { get {
        var ret = new double[NominalClaimsCashflow.Length];
        for (var i = NominalClaimsCashflow.Length - 1; i >= 0; i--)
            ret[i] = Math.Exp(-nonPerformanceRiskRate) * ret.ElementAtOrDefault(i + 1) + NominalClaimsCashflow[i] - NominalClaimsCashflow.ElementAtOrDefault(i + 1);
        return ret; } } 
        
    double[] NominalCashflow.Values => ArithmeticOperations.Subtract(PvCdrDecumulated, NominalClaimsCashflow);
}

public interface AllClaimsCashflow : NominalCashflow
{
    double[] NominalCashflow.Values => referenceAocSteps.SelectMany(refAocStep =>
                                        GetStorage().GetClaims()
                                        .Select(claim => GetStorage().GetValues(Identity.Id with {AocType = refAocStep.AocType, Novelty = refAocStep.Novelty}, claim, Identity.EstimateType, Identity.AccidentYear)))
                                        .AggregateDoubleArray();
}


public interface DiscountedCashflow : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<DiscountedCashflow>(s => s
            .WithApplicability<DiscountedCashflowNextYearsProjection>(x => x.Identity.Id.ProjectionPeriod > x.GetStorage().FirstNextYearProjection)
            .WithApplicability<DiscountedCreditRiskCashflow>(x => x.Identity.Id.IsReinsurance && x.GetStorage().GetCdr().Contains(x.Identity.AmountType)));

    private PeriodType periodType => GetStorage().GetPeriodType(Identity.AmountType, Identity.EstimateType); 

    private string EconomicBasis => GetContext();
    protected double[] MonthlyDiscounting => GetScope<MonthlyRate>(Identity.Id, o => o.WithContext(EconomicBasis)).Discount;
    protected double[] NominalValues => GetScope<NominalCashflow>(Identity).Values;

    double[] Values => ArithmeticOperations.Multiply(-1d, NominalValues.ComputeDiscountAndCumulate(MonthlyDiscounting, periodType)); // we need to flip the sign to create a reserve view
}

public interface DiscountedCreditRiskCashflow : DiscountedCashflow{
    private string cdrBasis => Identity.AmountType == AmountTypes.CDR ? EconomicBases.C : EconomicBases.L;
    private double nonPerformanceRiskRate => GetStorage().GetNonPerformanceRiskRate(Identity.Id, cdrBasis);
        
    double[] DiscountedCashflow.Values => ArithmeticOperations.Multiply(-1d, NominalValues.ComputeDiscountAndCumulateWithCreditDefaultRisk(MonthlyDiscounting, nonPerformanceRiskRate)); // we need to flip the sign to create a reserve view
}

public interface DiscountedCashflowNextYearsProjection : DiscountedCashflow{
    double[] DiscountedCashflow.Values => GetScope<DiscountedCashflow>((Identity.Id with {ProjectionPeriod = GetStorage().FirstNextYearProjection}, Identity.AmountType, Identity.EstimateType, Identity.AccidentYear)).Values;
}


public interface TelescopicDifference : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? Accidentyear), ImportStorage>
{
    [NotVisible]
    string EconomicBasis => GetContext();
    private double[] CurrentValues => GetScope<DiscountedCashflow>(Identity).Values;
    
    private double[] PreviousValues => (GetScope<ParentAocStep>((Identity.Id, Identity.AmountType, StructureType.AocPresentValue)))
                                            .Values
                                            .Select(aoc => GetScope<DiscountedCashflow>((Identity.Id with {AocType = aoc.AocType, Novelty = aoc.Novelty}, Identity.AmountType, Identity.EstimateType, Identity.Accidentyear)).Values)
                                            .Where(cf => cf.Count() > 0)
                                            .AggregateDoubleArray();
    
    double[] Values => ArithmeticOperations.Subtract(CurrentValues, PreviousValues);
}


public interface IWithGetValueFromValues : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    private int shift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    private int timeStep => 
        Identity.Id.LiabilityType == LiabilityTypes.LRC && 
        Identity.AccidentYear.HasValue && 
        (Consts.MonthInAYear * Identity.AccidentYear == (Consts.MonthInAYear * GetStorage().CurrentReportingPeriod.Year + GetStorage().GetShift(Identity.Id.ProjectionPeriod)))
    ? int.MaxValue
    : GetStorage().GetTimeStep(Identity.Id.ProjectionPeriod);

    public double GetValueFromValues(double[] Values, string overrideValuationPeriod = null)
    {
        var valuationPeriod = Enum.TryParse(overrideValuationPeriod, out ValuationPeriod ret) ? ret : GetStorage().GetValuationPeriod(Identity.Id);
        return valuationPeriod switch {
                        ValuationPeriod.BeginningOfPeriod => Values.ElementAtOrDefault(shift),
                        ValuationPeriod.MidOfPeriod => Values.ElementAtOrDefault(shift + Convert.ToInt32(Math.Round(timeStep / 2d, MidpointRounding.AwayFromZero)) - 1),
                        ValuationPeriod.Delta => Values.Skip(shift).Take(timeStep).Sum(),
                        ValuationPeriod.EndOfPeriod  => Values.ElementAtOrDefault(shift + timeStep),
                        ValuationPeriod.NotApplicable => default
                    };
    }
}


public interface IWithInterestAccretion : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    private double[] parentDiscountedValues => ArithmeticOperations.Multiply(-1d, GetScope<DiscountedCashflow>(Identity).Values);    
    private double[] parentNominalValues => GetScope<NominalCashflow>(Identity).Values;
    private double[] monthlyInterestFactor => GetScope<MonthlyRate>(Identity.Id).Interest;
    
    double[] GetInterestAccretion() 
    {
        if(!monthlyInterestFactor.Any())
            return Enumerable.Empty<double>().ToArray();
        var periodType = GetStorage().GetPeriodType(Identity.AmountType, Identity.EstimateType);
        var ret = new double[parentDiscountedValues.Length];
        
        switch (periodType) {
            case PeriodType.BeginningOfPeriod :
                for (var i = 0; i < parentDiscountedValues.Length; i++)
                     ret[i] = -1d * (parentDiscountedValues[i] - parentNominalValues[i]) * (monthlyInterestFactor.GetValidElement(i/12) - 1d );
                break;
            default :
                for (var i = 0; i < parentDiscountedValues.Length; i++)
                     ret[i] = -1d * parentDiscountedValues[i] * (monthlyInterestFactor.GetValidElement(i/12) - 1d );
             break;
        }
        
        return ret;
    }
}

public interface IWithInterestAccretionForCreditRisk : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    private double[] nominalClaimsCashflow => GetScope<AllClaimsCashflow>(Identity).Values;
    private double[] nominalValuesCreditRisk => ArithmeticOperations.Multiply(-1, GetScope<CreditDefaultRiskNominalCashflow>(Identity with {Id = Identity.Id with {AocType = AocTypes.CF}}).Values);
    private double[] monthlyInterestFactor => GetScope<MonthlyRate>(Identity.Id).Interest;
    private string cdrBasis => Identity.AmountType == AmountTypes.CDR ? EconomicBases.C : EconomicBases.L;
    private double nonPerformanceRiskRate => GetStorage().GetNonPerformanceRiskRate(Identity.Id, cdrBasis);
    
    double[] GetInterestAccretion() 
    {
        if(!monthlyInterestFactor.Any())
            return Enumerable.Empty<double>().ToArray();
            
        var interestOnClaimsCashflow = new double[nominalClaimsCashflow.Length];
        var interestOnClaimsCashflowCreditRisk = new double[nominalClaimsCashflow.Length];
        var effectCreditRisk = new double[nominalClaimsCashflow.Length];
        for (var i = nominalClaimsCashflow.Length - 1; i >= 0; i--) {
            interestOnClaimsCashflow[i] = 1 / monthlyInterestFactor.GetValidElement(i/12) * (interestOnClaimsCashflow.ElementAtOrDefault(i + 1) + nominalClaimsCashflow[i] - nominalClaimsCashflow.ElementAtOrDefault(i + 1));
            interestOnClaimsCashflowCreditRisk[i] = 1 / monthlyInterestFactor.GetValidElement(i/12) * (Math.Exp(-nonPerformanceRiskRate) * interestOnClaimsCashflowCreditRisk.ElementAtOrDefault(i + 1) + nominalClaimsCashflow[i] - nominalClaimsCashflow.ElementAtOrDefault(i + 1));
            effectCreditRisk[i] = interestOnClaimsCashflow[i] - interestOnClaimsCashflowCreditRisk[i];
        }
            
        return ArithmeticOperations.Subtract(nominalValuesCreditRisk, effectCreditRisk);
    }
}


public interface InterestAccretionFactor : IScope<ImportIdentity, ImportStorage>{
    private int timeStep => GetStorage().GetTimeStep(Identity.ProjectionPeriod);
    private int shift => GetStorage().GetShift(Identity.ProjectionPeriod);
    
    double GetInterestAccretionFactor(string economicBasis) 
    {
        double[] monthlyInterestFactor = GetScope<MonthlyRate>(Identity, o => o.WithContext(economicBasis)).Interest;
        return Enumerable.Range(shift,timeStep).Select(i => monthlyInterestFactor.GetValidElement(i/12)).Aggregate(1d, (x, y) => x * y ) - 1d;
    }
}


public interface NewBusinessInterestAccretion : IScope<ImportIdentity, ImportStorage>
{
    private int timeStep => GetStorage().GetTimeStep(Identity.ProjectionPeriod);
    private int shift => GetStorage().GetShift(Identity.ProjectionPeriod);

    double GetInterestAccretion(double[] values, string economicBasis) 
    {
        var monthlyInterestFactor = GetScope<MonthlyRate>(Identity, o => o.WithContext(economicBasis)).Interest;
        return values.NewBusinessInterestAccretion(monthlyInterestFactor, timeStep, shift);
    }
}


public interface PresentValue : IWithGetValueFromValues {   
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<PresentValue>(s => s
            .WithApplicability<ComputePresentValueWithIfrsVariable>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow || x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode))
            .WithApplicability<PresentValueFromDiscountedCashflow>(x => (x.Identity.Id.AocType == AocTypes.BOP && x.Identity.Id.Novelty != Novelties.C) || x.Identity.Id.AocType == AocTypes.EOP)
            .WithApplicability<CashflowAocStep>(x => x.Identity.Id.AocType == AocTypes.CF)
            .WithApplicability<PresentValueWithInterestAccretion>(x => x.Identity.Id.AocType == AocTypes.IA)
            .WithApplicability<EmptyValuesAocStep>(x => !x.GetStorage().GetAllAocSteps(StructureType.AocPresentValue).Contains(x.Identity.Id.AocStep) ||
                                                        (x.Identity.Id.AocType == AocTypes.CRU && !x.GetStorage().GetCdr().Contains(x.Identity.AmountType)) )
            );
    
    [NotVisible][IdentityProperty][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => GetContext();
    
    [NotVisible]
    double[] Values => GetScope<TelescopicDifference>(Identity).Values;
    
    public double Value => GetValueFromValues(Values);
}

public interface ComputePresentValueWithIfrsVariable : PresentValue {
    double PresentValue.Value => GetStorage().GetValue(Identity.Id, Identity.AmountType, Identity.EstimateType, EconomicBasis, Identity.AccidentYear, Identity.Id.ProjectionPeriod);
    double[] PresentValue.Values => Enumerable.Empty<double>().ToArray();
}

public interface PresentValueFromDiscountedCashflow : PresentValue {
    [NotVisible] double[] PresentValue.Values => GetScope<DiscountedCashflow>(Identity).Values;
}

public interface CashflowAocStep : PresentValue {
    [NotVisible] double[] PresentValue.Values => GetScope<NominalCashflow>(Identity).Values;
}

public interface PresentValueWithInterestAccretion : PresentValue, IWithInterestAccretion {
     static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<PresentValueWithInterestAccretion>(s => s.WithApplicability<PresentValueWithInterestAccretionForCreditRisk>(x => x.Identity.Id.IsReinsurance && x.GetStorage().GetCdr().Contains(x.Identity.AmountType)));    
    [NotVisible] double[] PresentValue.Values => GetInterestAccretion();
}

public interface PresentValueWithInterestAccretionForCreditRisk : PresentValue, IWithInterestAccretionForCreditRisk {
    [NotVisible] double[] PresentValue.Values => GetInterestAccretion();
}

public interface EmptyValuesAocStep : PresentValue {
    [NotVisible] double[] PresentValue.Values => Enumerable.Empty<double>().ToArray();
}


public interface PvAggregatedOverAccidentYear : IScope<(ImportIdentity Id, string AmountType, string EstimateType), ImportStorage>
{   
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => GetContext();
        
    private int?[] accidentYears => GetStorage().GetAccidentYears(Identity.Id.DataNode, Identity.Id.ProjectionPeriod).ToArray();  
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => accidentYears.Select(ay => 
        (Identity.AmountType, Identity.EstimateType, ay, GetScope<PresentValue>((Identity.Id, Identity.AmountType, Identity.EstimateType, ay), o => o.WithContext(EconomicBasis)).Value))
        .ToArray();
            
    double Value => PresentValues.Sum(pv => pv.Value);
}


public interface PvLocked : IScope<ImportIdentity, ImportStorage>
{   
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => EconomicBases.L;
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.BE;
       
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => GetScope<ValidAmountType>(Identity.DataNode).BeAmountTypes
        .SelectMany(at => GetScope<PvAggregatedOverAccidentYear>((Identity, at, EstimateType), o => o.WithContext(EconomicBasis)).PresentValues
        ).ToArray();
}


public interface PvCurrent : IScope<ImportIdentity, ImportStorage>
{
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => EconomicBases.C;
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.BE;

    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => GetScope<ValidAmountType>(Identity.DataNode).BeAmountTypes
        .SelectMany(at => GetScope<PvAggregatedOverAccidentYear>((Identity, at, EstimateType), o => o.WithContext(EconomicBasis)).PresentValues
        ).ToArray();
}


public interface CumulatedNominalBE : IScope<ImportIdentity, ImportStorage> {  
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => EconomicBases.N;
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.BE;
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => GetScope<ValidAmountType>(Identity.DataNode).BeAmountTypes
        .SelectMany(at => GetScope<PvAggregatedOverAccidentYear>((Identity, at, EstimateType), o => o.WithContext(EconomicBasis)).PresentValues
        ).ToArray();
}


public interface RaLocked : IScope<ImportIdentity, ImportStorage>
{   
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => EconomicBases.L;
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.RA;

    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => GetScope<PvAggregatedOverAccidentYear>((Identity, (string)null, EstimateType), o => o.WithContext(EconomicBasis)).PresentValues;
}


public interface RaCurrent : IScope<ImportIdentity, ImportStorage>
{
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => EconomicBases.C;
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.RA;
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => GetScope<PvAggregatedOverAccidentYear>((Identity, (string)null, EstimateType), o => o.WithContext(EconomicBasis)).PresentValues;
}


public interface CumulatedNominalRA : IScope<ImportIdentity, ImportStorage> {  
    [IdentityProperty][NotVisible][Dimension(typeof(EconomicBasis))]
    string EconomicBasis => EconomicBases.N;
    
    [IdentityProperty][NotVisible][Dimension(typeof(EstimateType))]
    string EstimateType => EstimateTypes.RA;
    
    [NotVisible]
    (string AmountType, string EstimateType, int? AccidentYear, double Value)[] PresentValues => GetScope<PvAggregatedOverAccidentYear>((Identity, (string)null, EstimateType), o => o.WithContext(EconomicBasis)).PresentValues;
}


public interface MonthlyAmortizationFactorCashflow : IScope<(ImportIdentity Id, string AmountType, int patternShift), ImportStorage>
{
    (string EffectiveAmountType, double[] Values) releasePattern => GetStorage().GetReleasePattern(Identity.Id, Identity.AmountType, Identity.patternShift);

    private PeriodType periodType => GetStorage().GetPeriodType(Identity.AmountType, EstimateTypes.P);
    private double[] monthlyDiscounting => GetScope<MonthlyRate>(Identity.Id).Discount;
    private double[] cdcPattern => releasePattern.Values.ComputeDiscountAndCumulate(monthlyDiscounting, periodType); 
    
    [NotVisible] string EconomicBasis => GetContext();
    
    double[] MonthlyAmortizationFactors => Identity.Id.AocType switch {
        AocTypes.AM when releasePattern.Values?.Any() ?? false => releasePattern.Values.Zip(cdcPattern,  //Extract to an other scope with month in the identity to avoid Zip?
            (nominal, discountedCumulated) => Math.Abs(discountedCumulated) >= Consts.Precision ? Math.Max(0, 1 - nominal / discountedCumulated) : 0).ToArray(),
        _ => Enumerable.Empty<double>().ToArray(),
        };
}


public interface CurrentPeriodAmortizationFactor : IScope<(ImportIdentity Id, string AmountType, int patternShift), ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<CurrentPeriodAmortizationFactor>(s => 
                 s.WithApplicability<AmfFromIfrsVariable>(x => x.GetStorage().ImportFormat != ImportFormats.Cashflow
                                                            || x.GetStorage().IsSecondaryScope(x.Identity.Id.DataNode)));

    private int shift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    private int timeStep => GetStorage().GetTimeStep(Identity.Id.ProjectionPeriod);
    private double amortizedFactor => GetScope<MonthlyAmortizationFactorCashflow>(Identity)
                            .MonthlyAmortizationFactors
                            .Skip(shift)
                            .Take(timeStep)
                            .DefaultIfEmpty()
                            .Aggregate(1d, (x, y) => x * y);
                            
    [NotVisible] string EconomicBasis => GetContext();

    string EstimateType => EstimateTypes.F;
    string EffectiveAmountType => GetScope<MonthlyAmortizationFactorCashflow>(Identity).releasePattern.EffectiveAmountType;
    double Value => 1d - amortizedFactor;
}

public interface AmfFromIfrsVariable : CurrentPeriodAmortizationFactor{
    private double amortizationFactorForAmountType => GetStorage().GetValue(Identity.Id, Identity.AmountType, EstimateType, EconomicBasis, 
        Identity.patternShift == 0 ? null : Identity.patternShift, Identity.Id.ProjectionPeriod); //TODO shift of 0 is a valid value
    
    private double amortizationFactorFromPattern => GetStorage().GetValue(Identity.Id, null, EstimateType, EconomicBasis, Identity.patternShift == 0 ? null : Identity.patternShift, Identity.Id.ProjectionPeriod);
    
    private double amortizationFactorForCu => GetStorage().GetValue(Identity.Id, AmountTypes.CU, EstimateType, EconomicBasis, 
        Identity.patternShift == 0 ? null : Identity.patternShift, Identity.Id.ProjectionPeriod);

    double CurrentPeriodAmortizationFactor.Value => Math.Abs(amortizationFactorForAmountType) >= Consts.Precision ? amortizationFactorForAmountType 
        : Math.Abs(amortizationFactorFromPattern) >= Consts.Precision ? amortizationFactorFromPattern : amortizationFactorForCu;
    string CurrentPeriodAmortizationFactor.EffectiveAmountType => Math.Abs(amortizationFactorForAmountType) >= Consts.Precision ? Identity.AmountType 
        : Math.Abs(amortizationFactorFromPattern) >= Consts.Precision ? null : AmountTypes.CU;
}



