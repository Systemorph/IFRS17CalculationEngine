using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ICurrentFcf : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> BestEstimate => GetScope<ICurrentBestEstimate>(Identity).CurrentBestEstimate;
    private IDataCube<ReportVariable> RiskAdjustment => GetScope<ICurrentRiskAdjustment>(Identity).CurrentRiskAdjustment;

    IDataCube<ReportVariable> CurrentFcf => BestEstimate + RiskAdjustment;
}