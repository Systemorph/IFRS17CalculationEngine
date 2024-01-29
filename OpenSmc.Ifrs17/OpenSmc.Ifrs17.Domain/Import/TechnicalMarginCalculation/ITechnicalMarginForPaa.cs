using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;
using OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForPaa : ITechnicalMargin
{
    [NotVisible] string ITechnicalMargin.EconomicBasis => EconomicBases.L;
    double ITechnicalMargin.Value => GetScope<ITechnicalMarginAmountType>((Identity, estimateType)).Values
                                         .Sum(at => GetScope<IPvAggregatedOverAccidentYear>((Identity, at, EstimateTypes.BE), o => o.WithContext(EconomicBasis)).Value) +
                                     GetScope<IPvAggregatedOverAccidentYear>((Identity, (string)null, EstimateTypes.RA), o => o.WithContext(EconomicBasis)).Value +
                                     GetScope<IDiscountedDeferrable>(Identity).Value + GetScope<IPremiumRevenue>(Identity).Value;
}