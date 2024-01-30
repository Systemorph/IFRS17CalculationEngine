using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IIncurredDeferrals : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> Deferrals => GetScope<IDeferrals>(Identity).Filter(("VariableType", "!BOP"), ("VariableType", "!EOP"));

    private IDataCube<ReportVariable> Amortization => -1 * Deferrals
        .Filter(("VariableType", AocTypes.AM));

    IDataCube<ReportVariable> AmortizationToIr => (-1 * Amortization).SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IR4" });
    IDataCube<ReportVariable> AmortizationToIse => Amortization.SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE6" });
}