using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IWrittenReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { nameof(EconomicBasis) };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "AmountType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<IWrittenAndAccruals>(GetIdentities()).Aggregate().Written
            : GetScopes<IWrittenAndAccruals>(GetIdentities()).Aggregate().Written.Filter(DataFilter);
}