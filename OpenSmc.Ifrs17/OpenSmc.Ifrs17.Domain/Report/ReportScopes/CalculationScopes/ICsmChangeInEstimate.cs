using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ICsmChangeInEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private (string amortization, string nonFinancial) VariableType => Identity.Id switch
    {
        { IsReinsurance: false } => ("IR3", "IR5"),
        { IsReinsurance: true } => ("ISE7", "ISE10")
    };

    private IDataCube<ReportVariable> Csm => GetScope<ICsm>(Identity).Csm.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) +
                                             GetScope<ICsm>(Identity).Csm.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I"));

    IDataCube<ReportVariable> Amortization => -1 * Csm.Filter(("VariableType", AocTypes.AM)).SelectToDataCube(v => v with { VariableType = VariableType.amortization });

    IDataCube<ReportVariable> NonFinancialChanges => -1 * Csm
        .Filter(("VariableType", "!AM"), ("VariableType", "!IA"), ("VariableType", "!YCU"), ("VariableType", "!CRU"), ("VariableType", "!FX"))
        .AggregateOver(nameof(Novelty), nameof(DataModel.KeyedDimensions.VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = VariableType.nonFinancial });

    IDataCube<ReportVariable> Fx => -1 * Csm.Filter(("VariableType", AocTypes.FX))
        .AggregateOver(nameof(Novelty))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE3" });

    IDataCube<ReportVariable> FinancialChanges => -1 * (Csm.Filter(("VariableType", AocTypes.IA)) +
                                                        Csm.Filter(("VariableType", AocTypes.YCU)) +
                                                        Csm.Filter(("VariableType", AocTypes.CRU)))
        .AggregateOver(nameof(Novelty), nameof(DataModel.KeyedDimensions.VariableType))
        .SelectToDataCube(v => v with { Novelty = Novelties.C, VariableType = "IFIE1" });
}