using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IDeferralReport : IIfrs17Report
{
    string[] IIfrs17Report.DefaultRowSlices => new[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<IDeferrals>(GetIdentities()).Aggregate().Deferrals
            : GetScopes<IDeferrals>(GetIdentities()).Aggregate().Deferrals.Filter(DataFilter);
}