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

public interface ILic : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> LicActuarial => GetScope<ILicActuarial>(Identity).LicActuarial;

    private IDataCube<ReportVariable> Accrual => GetScope<IWrittenAndAccruals>(Identity).Advance.Filter(("LiabilityType", LiabilityTypes.LIC)) +
                                                 GetScope<IWrittenAndAccruals>(Identity).Overdue.Filter(("LiabilityType", LiabilityTypes.LIC));

    private IDataCube<ReportVariable> LicData => LicActuarial + Accrual;

    private IDataCube<ReportVariable> Bop => LicData.Filter(("VariableType", AocTypes.BOP), ("Novelty", Novelties.I));

    private IDataCube<ReportVariable> Delta => (LicData.Filter(("VariableType", "!BOP"), ("VariableType", "!EOP")) + LicData.Filter(("VariableType", AocTypes.BOP), ("Novelty", "!I")))
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(x => Math.Abs(x.Value) >= Consts.Precision, x => x with { Novelty = Novelties.C, VariableType = "D" });

    private IDataCube<ReportVariable> Eop => LicData.Filter(("VariableType", AocTypes.EOP));

    IDataCube<ReportVariable> Lic => Bop + Delta + Eop;
}