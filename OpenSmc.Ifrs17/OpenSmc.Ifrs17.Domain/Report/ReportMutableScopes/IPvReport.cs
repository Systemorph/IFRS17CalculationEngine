using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IPvReport : IIfrs17Report
{
    string[] IIfrs17Report.DefaultRowSlices => new[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency", "EconomicBasis" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<LockedBestEstimate>(GetIdentities()).Aggregate().LockedBestEstimate +
                                GetScopes<CurrentBestEstimate>(GetIdentities()).Aggregate().CurrentBestEstimate
            : GetScopes<LockedBestEstimate>(GetIdentities()).Aggregate().LockedBestEstimate.Filter(DataFilter) +
              GetScopes<CurrentBestEstimate>(GetIdentities()).Aggregate().CurrentBestEstimate.Filter(DataFilter);
}