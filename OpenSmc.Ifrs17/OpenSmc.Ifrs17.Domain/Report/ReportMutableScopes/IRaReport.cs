using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IRaReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { "AmountType" };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency", "EconomicBasis" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<ILockedRiskAdjustment>(GetIdentities()).Aggregate().LockedRiskAdjustment +
                                GetScopes<ICurrentRiskAdjustment>(GetIdentities()).Aggregate().CurrentRiskAdjustment
            : GetScopes<ILockedRiskAdjustment>(GetIdentities()).Aggregate().LockedRiskAdjustment.Filter(DataFilter) +
              GetScopes<ICurrentRiskAdjustment>(GetIdentities()).Aggregate().CurrentRiskAdjustment.Filter(DataFilter);
}