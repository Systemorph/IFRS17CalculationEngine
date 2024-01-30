using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface ICsmReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { "AmountType", nameof(EconomicBasis) };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<Csm>(GetIdentities()).Aggregate().Csm +
                                GetScopes<Lc>(GetIdentities()).Aggregate().Lc +
                                GetScopes<Loreco>(GetIdentities()).Aggregate().Loreco
            : GetScopes<Csm>(GetIdentities()).Aggregate().Csm.Filter(DataFilter) +
              GetScopes<Lc>(GetIdentities()).Aggregate().Lc.Filter(DataFilter) +
              GetScopes<Loreco>(GetIdentities()).Aggregate().Loreco.Filter(DataFilter);
}