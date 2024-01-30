using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IBestEstimate : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    IDataCube<ReportVariable> BestEstimate => Identity.Id switch
    {
        { ValuationApproach: ValuationApproaches.PAA, LiabilityType: LiabilityTypes.LRC } => GetScope<ILockedBestEstimate>(Identity).LockedBestEstimate, //TODO we should use the economic basis driver to decide which Economic basis to use
        { ValuationApproach: ValuationApproaches.BBA, IsOci: true } => GetScope<ILockedBestEstimate>(Identity).LockedBestEstimate,
        _ => GetScope<ICurrentBestEstimate>(Identity).CurrentBestEstimate
    };
}