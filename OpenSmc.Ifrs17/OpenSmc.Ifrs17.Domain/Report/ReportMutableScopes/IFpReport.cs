using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IFpReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { "AmountType", nameof(EconomicBasis) };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "VariableType", "EstimateType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency" };
    int IIfrs17Report.HeaderColumnWidthValue => 500;
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<FinancialPerformance>(GetIdentities()).Aggregate().FinancialPerformance
            : GetScopes<FinancialPerformance>(GetIdentities()).Aggregate().FinancialPerformance.Filter(DataFilter);
}