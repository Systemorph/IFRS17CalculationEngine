using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IRiskAdjustment : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    IDataCube<ReportVariable> RiskAdjustment => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC } => GetScope<ILockedRiskAdjustment>(Identity).LockedRiskAdjustment, //TODO we should use the economic basis driver to decide which Economic basis to use
        { ValuationApproach: ValuationApproaches.BBA, IsOci: true } => GetScope<ILockedRiskAdjustment>(Identity).LockedRiskAdjustment,
        _ => GetScope<ICurrentRiskAdjustment>(Identity).CurrentRiskAdjustment
    };
}