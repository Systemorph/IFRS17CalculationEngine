using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;
using Systemorph.Vertex.Arithmetics.Aggregation;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface ICsmReport : IIfrs17Report
{
    string[] IIfrs17Report.ForbiddenSlices => new[] { "AmountType", nameof(EconomicBasis) };
    string[] IIfrs17Report.DefaultRowSlices => new[] { "Novelty", "VariableType" };
    string[] IIfrs17Report.DefaultColumnSlices => new[] { "Currency", "EstimateType" };
    IDataCube<ReportVariable> IIfrs17Report.GetDataCube() =>
        DataFilterRaw == null ? GetScopes<ICsm>(GetIdentities()).Aggregate().Csm +
                                GetScopes<ILc>(GetIdentities()).Aggregate().Lc +
                                GetScopes<ILoreco>(GetIdentities()).Aggregate().Loreco
            : GetScopes<ICsm>(GetIdentities()).Aggregate().Csm.Filter(DataFilter) +
              GetScopes<ILc>(GetIdentities()).Aggregate().Lc.Filter(DataFilter) +
              GetScopes<ILoreco>(GetIdentities()).Aggregate().Loreco.Filter(DataFilter);
}