using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ILorecoChangeInEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> Loreco => GetScope<ILoreco>(Identity).Loreco.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                                GetScope<ILoreco>(Identity).Loreco.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    IDataCube<ReportVariable> Amortization => -1 * Loreco.Filter(("VariableType", AocTypes.AM)).SelectToDataCube(v => v with { VariableType = "ISE8" });

    IDataCube<ReportVariable> NonFinancialChanges => -1 * Loreco
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });

    IDataCube<ReportVariable> Fx => -1 * Loreco.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { VariableType = "IFIE3" });

    IDataCube<ReportVariable> FinancialChangesToIse => -1 * (Loreco.Filter(("VariableType", AocTypes.IA)) +
                                                             Loreco.Filter(("VariableType", AocTypes.YCU)) +
                                                             Loreco.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "ISE11" });
}