using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForAmForPaa : ITechnicalMargin
{
    private IEnumerable<string> Novelties => GetStorage().GetNovelties(AocTypes.CF, StructureType.AocPresentValue);
    double ITechnicalMargin.Value => GetScope<ITechnicalMarginAmountType>((Identity, estimateType)).Values
        .Sum(at => Novelties.Sum(n => GetScope<IPvAggregatedOverAccidentYear>((Identity with { AocType = AocTypes.CF, Novelty = n }, at, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value +
                                      GetScope<IPvAggregatedOverAccidentYear>((Identity with { AocType = AocTypes.CF, Novelty = n }, (string)null, EstimateTypes.RA), o => o.WithContext(EconomicBasis)).Value));
    //+  Revenue AM + Deferral AM

}