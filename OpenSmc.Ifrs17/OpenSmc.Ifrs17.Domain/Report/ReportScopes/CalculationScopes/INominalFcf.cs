using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface INominalFcf : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> BestEstimate => GetScope<INominalBestEstimate>(Identity).NominalBestEstimate;
    private IDataCube<ReportVariable> RiskAdjustment => GetScope<INominalRiskAdjustment>(Identity).NominalRiskAdjustment;

    IDataCube<ReportVariable> NominalFcf => BestEstimate + RiskAdjustment;
}