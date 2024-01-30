using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface ITmReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { "AmountType", nameof(EconomicBasis) };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<LrcTechnicalMargin>(GetIdentities()).Aggregate().LrcTechnicalMargin
            : GetScopes<LrcTechnicalMargin>(GetIdentities()).Aggregate().LrcTechnicalMargin.Filter(DataFilter);
}