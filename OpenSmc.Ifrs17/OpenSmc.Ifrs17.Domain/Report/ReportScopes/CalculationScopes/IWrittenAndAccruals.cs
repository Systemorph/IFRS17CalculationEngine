using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IWrittenAndAccruals : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    IDataCube<ReportVariable> Written => GetScope<IFxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.A)).FxData;
    IDataCube<ReportVariable> Advance => GetScope<IFxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.AA)).FxData;
    IDataCube<ReportVariable> Overdue => GetScope<IFxData>((Identity.Id, Identity.CurrencyType, EstimateTypes.OA)).FxData;
}