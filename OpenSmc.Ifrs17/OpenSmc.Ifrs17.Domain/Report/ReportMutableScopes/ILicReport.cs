using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface ILicReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { "AmountType" };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<Lic>(GetIdentities()).Aggregate().Lic
            : GetScopes<Lic>(GetIdentities()).Aggregate().Lic.Filter(DataFilter);
}