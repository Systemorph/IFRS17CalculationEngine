using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IFcf : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> BestEstimate => GetScope<IBestEstimate>(Identity).BestEstimate;
    private IDataCube<ReportVariable> RiskAdjustment => GetScope<IRiskAdjustment>(Identity).RiskAdjustment;

    IDataCube<ReportVariable> Fcf => BestEstimate + RiskAdjustment;
}