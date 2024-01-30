using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IWrittenReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { nameof(EconomicBasis) };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "AmountType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<WrittenAndAccruals>(GetIdentities()).Aggregate().Written
            : GetScopes<WrittenAndAccruals>(GetIdentities()).Aggregate().Written.Filter(DataFilter);
}