using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public interface IData : IMutableScope<((int year, int month) period, string reportingNode, string scenario, CurrencyType currencyType, string reportType, (string filterName, object filterValue)[] dataFilter)>
{
    IDataCube<ReportVariable> Cube
    {
        get
        {
            var data = GetScope<IIfrs17Report>(Identity.reportType).GetDataCube();
            // TODO: suggestion to place the filter here instead of having it in every applicability scope
            if (Identity.scenario != Scenarios.All && Identity.scenario != Scenarios.Delta) return data;
            if (Identity.scenario == Scenarios.All) return data?.Select(x => x.Scenario == null ? x with { Scenario = Scenarios.Default } : x).ToDataCube();
            var bestEstimateById = data?.Where(x => x.Scenario == null).ToDictionary(x => x.ToIdentityString());
            return data?.Select(x => x.Scenario == null ? x with { Scenario = Scenarios.Default }
                : x with { Value = x.Value - (bestEstimateById.TryGetValue((x with { Scenario = null }).ToIdentityString(), out var be) ? be.Value : 0.0) }).ToDataCube();
        }
    }
}