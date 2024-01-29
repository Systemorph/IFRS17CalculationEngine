namespace OpenSmc.Ifrs17.Domain.Import.PremiumRevenueCalculation;

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

//only PAA LRC

// public interface PremiumRevenueForIaNewBusiness : IPremiumRevenue, NewBusinessInterestAccretion {
//     private double[] nominalCashflows => GetStorage().GetPremiums().Select(at => 
//         GetScope<NominalCashflow>((Identity, at, EstimateTypes.BE, (int?)null)).Values).AggregateDoubleArray();

//     double IPremiumRevenue.Value => GetStorage().ImportFormat != ImportFormats.Cashflow || GetStorage().IsSecondaryScope(Identity.DataNode) // This is normally an applicability for the scope, but this is the only case --> to be re-checked
//         ? GetStorage().GetValue(Identity, null, EstimateType, EconomicBasis, (int?)null, Identity.ProjectionPeriod)
//         : -1d * GetInterestAccretion(nominalCashflows, EconomicBasis);
// }

public interface IPremiumRevenueEop : IPremiumRevenue
{
    double IPremiumRevenue.Value => GetScope<AggregatedIPremiumRevenue>(Identity).AggregatedValue;
}



