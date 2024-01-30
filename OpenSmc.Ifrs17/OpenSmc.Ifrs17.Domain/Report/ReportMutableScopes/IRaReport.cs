using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IRaReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { "AmountType" };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency", "EconomicBasis" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<LockedRiskAdjustment>(GetIdentities()).Aggregate().LockedRiskAdjustment +
                                GetScopes<CurrentRiskAdjustment>(GetIdentities()).Aggregate().CurrentRiskAdjustment
            : GetScopes<LockedRiskAdjustment>(GetIdentities()).Aggregate().LockedRiskAdjustment.Filter(DataFilter) +
              GetScopes<CurrentRiskAdjustment>(GetIdentities()).Aggregate().CurrentRiskAdjustment.Filter(DataFilter);
}