using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IExpAdjReport : IIfrs17Report
{
    string[] IIfrs17Report.DefaultRowSlices => new[] { "AmountType", "EstimateType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<IExperienceAdjustment>(GetIdentities()).Aggregate().ActuarialExperienceAdjustment
            : GetScopes<IExperienceAdjustment>(GetIdentities()).Aggregate().ActuarialExperienceAdjustment.Filter(DataFilter);
}