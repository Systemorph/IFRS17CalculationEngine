using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;
using OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;
using OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

// public interface DeferrableForIaNewBusiness : IDiscountedDeferrable, NewBusinessInterestAccretion {
//     private double[] nominalCashflows => GetStorage().GetDeferrableExpenses().Select(at => 
//         GetScope<NominalCashflow>((Identity, at, EstimateTypes.BE, (int?)null)).Values).AggregateDoubleArray();

//     double IDiscountedDeferrable.Value => GetStorage().ImportFormat != ImportFormats.Cashflow || GetStorage().IsSecondaryScope(Identity.DataNode) // This is normally an applicability for the scope, but this is the only case --> to be re-checked
//         ? GetStorage().GetValue(Identity, null, EstimateType, EconomicBasis, (int?)null, Identity.ProjectionPeriod)
//         : -1d * GetInterestAccretion(nominalCashflows, EconomicBasis);
// }

//TODO : 
// EstimateType from DA to DAC
// BOP,I only through Opening. 

public interface IAmReferenceDeferrable: IScope<(ImportIdentity Id, int MonthlyShift), ImportStorage>{
    private int projectionShift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    private IEnumerable<AocStep> previousAocSteps => GetScope<IPreviousAocSteps>((Identity.Id, StructureType.AocTechnicalMargin)).Values.Where(aocStep => aocStep.Novelty != Novelties.C);
    double referenceCashflow => previousAocSteps
        .GroupBy(x => x.Novelty, (k, aocs) => aocs.Last())
        .Sum(aoc => GetScope<INominalCashflow>((Identity.Id with {AocType = aoc.AocType, Novelty = aoc.Novelty}, AmountTypes.DAE, EstimateTypes.BE, (int?)null)).Values
        .Skip(projectionShift + Identity.MonthlyShift).FirstOrDefault());
    //if no previous RawVariable, use IfrsVariable
    double Value => Math.Abs(referenceCashflow) >= Consts.Precision ? referenceCashflow : GetStorage().GetNovelties(AocTypes.BOP, StructureType.AocPresentValue).Sum(n => GetScope<INominalDeferrable>((Identity.Id with {AocType = AocTypes.BOP, Novelty = n}, Identity.MonthlyShift)).Value);
}

public interface IAmDeferrable : INominalDeferrable{
    private IEnumerable<AocStep> referenceAocSteps => GetScope<IReferenceAocStep>(Identity.Id).Values; //Reference step of AM,C is CL,C
    private double referenceCashflow => referenceAocSteps.Sum(refAocStep => GetScope<IAmReferenceDeferrable>((Identity.Id with {AocType = refAocStep.AocType, Novelty = refAocStep.Novelty}, Identity.MonthlyShift)).Value);

    double INominalDeferrable.Value => Math.Abs(referenceCashflow) > Consts.Precision ? -1d * referenceCashflow * GetScope<ICurrentPeriodAmortizationFactor>((Identity.Id, AmountTypes.DAE, Identity.MonthlyShift), o => o.WithContext(EconomicBasis)).Value : default;
}

public interface IEopDeferrable : INominalDeferrable{
    private IEnumerable<AocStep> previousAocSteps => GetScope<IPreviousAocSteps>((Identity.Id, StructureType.AocTechnicalMargin)).Values;
    double INominalDeferrable.Value => previousAocSteps.Sum(aocStep => GetScope<INominalDeferrable>((Identity.Id with {AocType = aocStep.AocType, Novelty = aocStep.Novelty}, Identity.MonthlyShift)).Value);
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



