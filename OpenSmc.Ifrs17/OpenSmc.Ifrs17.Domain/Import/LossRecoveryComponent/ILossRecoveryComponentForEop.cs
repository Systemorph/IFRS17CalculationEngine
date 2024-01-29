using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.LossRecoveryComponent;

// public interface TechnicalMarginForIaNewBusiness : ITechnicalMargin, NewBusinessInterestAccretion {
//     private int?[] accidentYears => GetStorage().GetAccidentYears(Identity.DataNode, Identity.ProjectionPeriod).ToArray();
//     private string[] amountTypes => GetScope<ITechnicalMarginAmountType>((Identity, estimateType)).Values.ToArray();

//     private double[] nominalCashflows => accidentYears.SelectMany(ay =>
//         amountTypes.Select(at => GetScope<NominalCashflow>((Identity, at, EstimateTypes.BE, ay)).Values))
//         .AggregateDoubleArray()
//         .Concat(GetScope<NominalCashflow>((Identity, (string)null, EstimateTypes.RA, (int?)null)).Values)
//         .ToArray();

//     double ITechnicalMargin.Value => GetStorage().ImportFormat != ImportFormats.Cashflow || GetStorage().IsSecondaryScope(Identity.DataNode) // This is normally an applicability for the scope, but this is the only case --> to be re-checked
//         ? (estimateType == EstimateTypes.LR) 
//             ? GetStorage().GetValue(Identity, null, estimateType, EconomicBasis, (int?)null, Identity.ProjectionPeriod)
//             : new [] {EstimateTypes.C, EstimateTypes.L}.Select(et => GetStorage().GetValue(Identity, null, et, EconomicBasis, (int?)null, Identity.ProjectionPeriod)).Sum()
//         : GetInterestAccretion(nominalCashflows, EconomicBasis);  
// }

public interface ILossRecoveryComponentForEop : ILossRecoveryComponent
{
    double ILossRecoveryComponent.Value => GetScope<IPreviousAocSteps>((Identity, StructureType.AocTechnicalMargin)).Values
                                                .Sum(aoc => GetScope<ILossRecoveryComponent>(Identity with { AocType = aoc.AocType, Novelty = aoc.Novelty }).Value);
}
