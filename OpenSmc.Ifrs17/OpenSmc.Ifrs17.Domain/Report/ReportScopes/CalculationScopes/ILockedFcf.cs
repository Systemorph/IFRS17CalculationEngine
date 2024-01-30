using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ILockedFcf : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> BestEstimate => GetScope<ILockedBestEstimate>(Identity).LockedBestEstimate;
    private IDataCube<ReportVariable> RiskAdjustment => GetScope<ILockedRiskAdjustment>(Identity).LockedRiskAdjustment;

    IDataCube<ReportVariable> LockedFcf => BestEstimate + RiskAdjustment;
}