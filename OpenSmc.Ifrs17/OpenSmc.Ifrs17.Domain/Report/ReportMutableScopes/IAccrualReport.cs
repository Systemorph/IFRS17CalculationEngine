using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IAccrualReport : IIfrs17Report
{
    string[] IIfrs17Report.DefaultRowSlices => new[] { "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<IWrittenAndAccruals>(GetIdentities()).Aggregate().Advance +
                                GetScopes<IWrittenAndAccruals>(GetIdentities()).Aggregate().Overdue
            : GetScopes<IWrittenAndAccruals>(GetIdentities()).Aggregate().Advance.Filter(DataFilter) +
              GetScopes<IWrittenAndAccruals>(GetIdentities()).Aggregate().Overdue.Filter(DataFilter);
}