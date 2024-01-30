using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Grid.Model;
using Systemorph.Vertex.Scopes;
using Systemorph.Vertex.Workspace;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

[InitializeScope(nameof(InitAsync))]
public interface IIfrs17Report : IMutableScope<string, ReportStorage>
{
    // Infrastructure
    protected IWorkspace Workspace => GetStorage().Workspace;
    protected Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory Report => GetStorage().Report;

    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder) =>
        builder.ForScope<IIfrs17Report>(s => s.WithApplicability<IPvReport>(x => x.Identity == nameof(IPvReport))
            .WithApplicability<IRaReport>(x => x.Identity == nameof(IRaReport))
            .WithApplicability<IWrittenReport>(x => x.Identity == nameof(IWrittenReport))
            .WithApplicability<IAccrualReport>(x => x.Identity == nameof(IAccrualReport))
            .WithApplicability<IDeferralReport>(x => x.Identity == nameof(IDeferralReport))
            .WithApplicability<IFcfReport>(x => x.Identity == nameof(IFcfReport))
            .WithApplicability<IExpAdjReport>(x => x.Identity == nameof(IExpAdjReport))
            .WithApplicability<ITmReport>(x => x.Identity == nameof(ITmReport))
            .WithApplicability<ICsmReport>(x => x.Identity == nameof(ICsmReport))
            .WithApplicability<IActLrcReport>(x => x.Identity == nameof(IActLrcReport))
            .WithApplicability<ILrcReport>(x => x.Identity == nameof(ILrcReport))
            .WithApplicability<IActLicReport>(x => x.Identity == nameof(IActLicReport))
            .WithApplicability<ILicReport>(x => x.Identity == nameof(ILicReport))
            .WithApplicability<IFpReport>(x => x.Identity == nameof(IFpReport))
            .WithApplicability<IFpAlternativeReport>(x => x.Identity == nameof(IFpAlternativeReport))
        );

    // Basic mutable properties
    (int Year, int Month) ReportingPeriod { get; set; }
    int Year => ReportingPeriod.Year;
    int Month => ReportingPeriod.Month;
    string ReportingNode { get; set; }
    string? Scenario { get; set; }  // TODO: Enable dropdown selection including All and Delta
    string Comparison { get; set; }  // TODO: only for scenario at the beginning, meant to enable general purpose comparisons 
    CurrencyType CurrencyType { get; set; }
    string? Projection { get; set; } //NB input is P0 or P1 or P15...

    private ProjectionConfiguration[] ProjectionConfigurations => GetStorage().ProjectionConfigurations.SortRelevantProjections(Month);
    private string? LastValidProjection => Projection != null ? "P" + ProjectionConfigurations.Select(x => x.SystemName.Split("P")[1]).Take(int.Parse(Projection.Split("P")[1]) + 1).Last() : null;
    ((int Year, int Month) ReportingPeriod, string? ReportingNode, string? Scenario, CurrencyType) ShowSettings => (ReportingPeriod, ReportingNode, Scenario, CurrencyType);

    // Slice and Dice
    protected string[] ForbiddenSlices => new string[] { };

    IEnumerable<string>? RowSlicesRaw { get; set; }
    protected string[] DefaultRowSlices => new string[] { };
    protected string[]? RowSlices => RowSlicesRaw is null
        ? DefaultRowSlices
        : DefaultRowSlices.Where(cs => !RowSlicesRaw.Contains(cs)).Concat(RowSlicesRaw).Where(x => !ForbiddenSlices.Contains(x)).ToArray();

    IEnumerable<string>? ColumnSlicesRaw { get; set; }
    protected string[] DefaultColumnSlices => new string[] { };
    protected string[] ColumnSlices
    {
        get
        {
            var defaultColumnSlicesWithProjection = Projection == null ? DefaultColumnSlices : nameof(ReportVariable.Projection).RepeatOnce().Concat(DefaultColumnSlices).ToArray();
            var slices = ColumnSlicesRaw is null
                ? defaultColumnSlicesWithProjection
                : defaultColumnSlicesWithProjection.Where(cs => !ColumnSlicesRaw.Contains(cs)).Concat(ColumnSlicesRaw).Where(x => !ForbiddenSlices.Contains(x)).ToArray();
            return Scenario == Scenarios.All || Scenario == Scenarios.Delta
                ? slices.Concat(nameof(Scenario).RepeatOnce()).ToArray()
                : Scenario is null ? slices : nameof(Scenario).RepeatOnce().Concat(slices).ToArray();
        }
    }

    // Identities
    protected HashSet<(ReportIdentity, CurrencyType)> GetIdentities() => GetStorage().GetIdentities(ReportingPeriod, ReportingNode, Scenario, CurrencyType, LastValidProjection);

    // Filter
    IEnumerable<(string filterName, string filterValue)>? DataFilterRaw { get; set; }
    protected (string filterName, object filterValue)[] DataFilter => (DataFilterRaw is null ? Enumerable.Empty<(string, object)>() : DataFilterRaw.Select(x => (x.filterName, (object)x.filterValue))).ToArray();

    // Scope Initialization
    async Task InitAsync()
    {
        var mostRecentPartition = (await Workspace.Query<PartitionByReportingNodeAndPeriod>().Where(x => x.Scenario == null).OrderBy(x => x.Year).ThenBy(x => x.Month).ToArrayAsync()).Last();
        ReportingPeriod = (mostRecentPartition.Year, mostRecentPartition.Month);
        ReportingNode = (await Workspace.Query<ReportingNode>().Where(x => x.Parent == null).ToArrayAsync()).First().SystemName; // TODO: change once user permissions are available
        Scenario = null;
        CurrencyType = CurrencyType.Contractual;
        await GetStorage().InitializeReportIndependentCacheAsync();
    }

    //Report
    public IDataCube<ReportVariable>? GetDataCube() => default;
    protected int HeaderColumnWidthValue => 250;
    public Task<GridOptions> ToReportAsync => GetReportTaskAsync();
    private async Task<GridOptions> GetReportTaskAsync()
    {
        await GetStorage().InitializeAsync(ReportingPeriod, ReportingNode, Scenario, CurrencyType);
        return await Report.ForDataCube(GetScope<IData>((ReportingPeriod, ReportingNode, Scenario, CurrencyType, Identity, DataFilter)).Cube)
            .WithQuerySource(Workspace)
            .SliceRowsBy(RowSlices)
            .SliceColumnsBy(ColumnSlices)
            .ReportGridOptions(headerColumnWidth: HeaderColumnWidthValue)
            .ExecuteAsync();
    }
}