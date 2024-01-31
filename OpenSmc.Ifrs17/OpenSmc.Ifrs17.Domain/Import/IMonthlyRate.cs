using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IMonthlyRate : IScope<ImportIdentity, ImportStorage>
{
    private string EconomicBasis => GetContext();
    
    private double[] YearlyYieldCurve => EconomicBasis switch {
        EconomicBases.N => new [] { 0d },
        _ => GetStorage().GetYearlyYieldCurve(Identity, EconomicBasis),
    };
    
    double[] Interest => YearlyYieldCurve.Select(rate => Math.Pow(1d + rate, 1d / 12d)).ToArray();   
                        
    double[] Discount => Interest.Select(x => Math.Pow(x, -1)).ToArray();
}