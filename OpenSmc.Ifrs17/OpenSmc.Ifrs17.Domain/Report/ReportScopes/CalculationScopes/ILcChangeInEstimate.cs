using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ILcChangeInEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> Lc => GetScope<ILc>(Identity).Lc.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                            GetScope<ILc>(Identity).Lc.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    IDataCube<ReportVariable> Amortization => -1 * Lc.Filter(("VariableType", AocTypes.AM)).SelectToDataCube(v => v with { VariableType = "ISE9" });

    IDataCube<ReportVariable> NonFinancialChanges => -1 * Lc
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    IDataCube<ReportVariable> NonFinancialChangesToIr => -1 * (Amortization + NonFinancialChanges).SelectToDataCube(v => v with { VariableType = "IR5" });

    IDataCube<ReportVariable> Fx => -1 * Lc.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    IDataCube<ReportVariable> FinancialChanges => 1 * (Lc.Filter(("VariableType", AocTypes.IA)) +
                                                       Lc.Filter(("VariableType", AocTypes.YCU)) +
                                                       Lc.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });

    IDataCube<ReportVariable> FinancialChangesToIse => -1 * FinancialChanges.SelectToDataCube(v => v with { VariableType = "ISE11" });
}