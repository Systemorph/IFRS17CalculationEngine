using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IActLicReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { "AmountType" };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<ILicActuarial>(GetIdentities()).Aggregate().LicActuarial
            : GetScopes<ILicActuarial>(GetIdentities()).Aggregate().LicActuarial.Filter(DataFilter);
}