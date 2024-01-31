using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMargin : IScope<ImportIdentity, ImportStorage>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<ITechnicalMargin>(s => s.WithApplicability<ITechnicalMarginForCurrentBasis>(x => x.Identity.ValuationApproach == ValuationApproaches.VFA, p => p.ForMember(s => s.EconomicBasis))
            .WithApplicability<ITechnicalMarginForBopProjection>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && x.Identity.ProjectionPeriod > 0)
            .WithApplicability<ITechnicalMarginForBop>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I)
            .WithApplicability<ITechnicalMarginDefaultValue>(x => x.Identity.AocType == AocTypes.CF)
            .WithApplicability<ITechnicalMarginForIaStandard>(x => x.Identity.AocType == AocTypes.IA)// && x.Identity.Novelty == Novelties.I)
                                                                                                     //.WithApplicability<TechnicalMarginForIaNewBusiness>(x => x.Identity.AocType == AocTypes.IA)
            .WithApplicability<ITechnicalMarginForEa>(x => x.Identity.AocType == AocTypes.EA && !x.Identity.IsReinsurance)
            .WithApplicability<ITechnicalMarginForAm>(x => x.Identity.AocType == AocTypes.AM)
            .WithApplicability<ITechnicalMarginForEop>(x => x.Identity.AocType == AocTypes.EOP)
            .WithApplicability<ITechnicalMarginForPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA)
        );

    protected string estimateType => GetContext();
    [NotVisible] string EconomicBasis => EconomicBases.L;
    double Value => GetScope<ITechnicalMarginAmountType>((Identity, estimateType)).Values
                        .Sum(at => GetScope<IPvAggregatedOverAccidentYear>((Identity, at, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value) +
                    GetScope<IPvAggregatedOverAccidentYear>((Identity, (string)null, EstimateTypes.RA), o => o.WithContext(EconomicBasis)).Value;

    double AggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => GetScope<ITechnicalMargin>(Identity with { AocType = aoc.AocType, Novelty = aoc.Novelty }).Value);
}