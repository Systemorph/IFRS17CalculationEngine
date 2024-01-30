using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IFx : IScope<(string ContractualCurrency, string FunctionalCurrency, FxPeriod FxPeriod, (int, int) Period, CurrencyType CurrencyType), ReportStorage>
{
    private double GroupFxRate => Identity.CurrencyType switch
    {
        CurrencyType.Group => GetStorage().GetFx(Identity.Period, Identity.FunctionalCurrency, Consts.GroupCurrency, FxPeriod.Average),
        _ => 1
    };

    private double GetFunctionalFxRate(FxPeriod fxPeriod)
    {
        return Identity.CurrencyType switch
        {
            CurrencyType.Contractual => 1,
            _ => GetStorage().GetFx(Identity.Period, Identity.ContractualCurrency, Identity.FunctionalCurrency, fxPeriod)
        };
    }

    double Fx => GetFunctionalFxRate(Identity.FxPeriod) * GroupFxRate;
}