using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IDeferralReport : IIfrs17Report
{
    string[] IIfrs17Report.DefaultRowSlices => new[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<Deferrals>(GetIdentities()).Aggregate().Deferrals
            : GetScopes<Deferrals>(GetIdentities()).Aggregate().Deferrals.Filter(DataFilter);
}