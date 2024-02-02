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

public interface ILrc : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    protected IDataCube<ReportVariable> LrcActuarial => GetScope<ILrcActuarial>(Identity).LrcActuarial;

    protected IDataCube<ReportVariable> Accrual => GetScope<IWrittenAndAccruals>(Identity).Advance.Filter(("LiabilityType", LiabilityTypes.LRC)) +
                                                   GetScope<IWrittenAndAccruals>(Identity).Overdue.Filter(("LiabilityType", LiabilityTypes.LRC));

    protected IDataCube<ReportVariable> LrcData => LrcActuarial + Accrual;

    private IDataCube<ReportVariable> Bop => LrcData.Filter(("VariableType", AocTypes.BOP), ("Novelty", Novelties.I));

    private IDataCube<ReportVariable> Delta => (LrcData.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) + LrcData.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I")))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(x => Math.Abs(x.Value) >= Consts.Precision, x => x with { Novelty = Novelties.C, VariableType = "D" });

    private IDataCube<ReportVariable> Eop => LrcData.Filter(("VariableType", AocTypes.EOP));

    IDataCube<ReportVariable> Lrc => Bop + Delta + Eop;
}