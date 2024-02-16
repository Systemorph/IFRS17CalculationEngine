using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import.LossRecoveryComponent;

public interface ILossRecoveryComponent : IScope<ImportIdentity, ImportStorageOld>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<ILossRecoveryComponent>(s => s
            .WithApplicability<ILossRecoveryComponentForBopProjection>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && x.Identity.ProjectionPeriod > 0)
            .WithApplicability<ILossRecoveryComponentForBop>(x => x.Identity.AocType == AocTypes.BOP && x.Identity.Novelty == Novelties.I && !x.GetStorage().IsInceptionYear(x.Identity.DataNode))
            .WithApplicability<ILossRecoveryComponentPaa>(x => x.Identity.ValuationApproach == ValuationApproaches.PAA || x.Identity.AocType == AocTypes.BOP)
            .WithApplicability<ILossRecoveryComponentForAm>(x => x.Identity.AocType == AocTypes.AM)
            .WithApplicability<ILossRecoveryComponentForEop>(x => x.Identity.AocType == AocTypes.EOP)
        );

    protected double LoReCoBoundaryValue => GetScope<ILoReCoBoundary>(Identity).Value;
    private double AggregatedLoReCoBoundary => GetScope<ILoReCoBoundary>(Identity).AggregatedValue;
    private double ReinsuranceCsm => GetScope<ITechnicalMargin>(Identity, o => o.WithContext(EstimateType)).Value;
    private double AggregatedLoReCoProjectionWithFm => AggregatedValue + ReinsuranceCsm;
    private bool IsAboveUpperBoundary => AggregatedLoReCoProjectionWithFm >= Consts.Precision;
    private bool IsBelowLowerBoundary => AggregatedLoReCoProjectionWithFm < -1d * (AggregatedLoReCoBoundary + LoReCoBoundaryValue);
    private double MarginToLowerBoundary => -1d * (AggregatedValue + AggregatedLoReCoBoundary + LoReCoBoundaryValue);

    protected double AggregatedValue => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
        .Sum(aoc => GetScope<ILossRecoveryComponent>(Identity with { AocType = aoc.AocType, Novelty = aoc.Novelty }).Value);

    double Value => (isAboveUpperBoundary: IsAboveUpperBoundary, isBelowLowerBoundary: IsBelowLowerBoundary) switch
    {
        (false, false) => ReinsuranceCsm,
        (false, true) => MarginToLowerBoundary,
        (true, false) => -1d * AggregatedValue,
        _ => default
    };

    [NotVisible] string EstimateType => EstimateTypes.LR;
}