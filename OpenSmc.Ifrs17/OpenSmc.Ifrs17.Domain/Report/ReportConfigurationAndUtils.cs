using Systemorph.Vertex.Pivot.Builder;
using System.Collections.Immutable;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Attributes.Arithmetics;
using Systemorph.Vertex.Collections;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Pivot.Reporting;
using Systemorph.Vertex.Pivot.Reporting.Builder;
using Systemorph.Vertex.Workspace;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;

namespace OpenSmc.Ifrs17.Domain.Report;

public static class ReportConfigExtensions
{
    public static DataCubeReportBuilder<IDataCube<TVariable>, TVariable, TVariable, TVariable>
        ReportGridOptions<TVariable>(
            this DataCubePivotBuilder<IDataCube<TVariable>, TVariable, TVariable, TVariable> pivotBuilder,
            int reportHeight = 700,
            int valueColumnWidth = 250,
            int headerColumnWidth = 250,
            int groupDefaultExpanded = 2)
        => pivotBuilder.ToTable().WithOptions(go => go
                .WithColumns(cols => cols.Modify("Value",
                    c => c.WithWidth(valueColumnWidth)
                        .WithFormat(
                            "typeof(value) == 'number' ? new Intl.NumberFormat('en',{ minimumFractionDigits:2, maximumFractionDigits:2 }).format(value) : value")))
                .WithRows(rows => rows
                    .Where(r => !(r.RowGroup.Coordinates.Last() == "NullGroup"))
                    .Select(r => r with
                    {
                        RowGroup = r.RowGroup with
                        {
                            Coordinates = r.RowGroup.Coordinates.Where(c => c != "NullGroup").ToImmutableList()
                        }
                    })
                    .ToArray())
                .HideRowValuesForDimension("Novelty")
                .WithAutoGroupColumn(c => c.WithWidth(headerColumnWidth) with {Pinned = "left"})
            with
            {
                Height = reportHeight, GroupDefaultExpanded = groupDefaultExpanded, OnGridReady = null
            });


    public static double GetCurrencyToGroupFx(Dictionary<string, Dictionary<FxPeriod, double>> exchangeRates,
        string currency, FxPeriod fxPeriod, string groupCurrency)
    {
        if (currency == groupCurrency)
            return 1;

        if (!exchangeRates.TryGetValue(currency, out var currencyToGroup))
            ApplicationMessage.Log(Error.ExchangeRateCurrency, currency);

        if (!currencyToGroup.TryGetValue(fxPeriod, out var currencyToGroupFx))
            ApplicationMessage.Log(Error.ExchangeRateNotFound, currency, fxPeriod.ToString());

        return currencyToGroupFx;
    }


    public static IEnumerable<ReportVariable> GetReportVariable(GroupOfContract goc,
        IfrsVariable iv,
        (int Year, int Month, string ReportingNode, string Scenario) args,
        ProjectionConfiguration[] orderedProjectionConfigurations) =>
        iv.Values.Select((val, ind) => new ReportVariable
        {
            ReportingNode = args.ReportingNode,
            Scenario = args.Scenario,
            Portfolio = goc.Portfolio,
            GroupOfContract = goc.SystemName,
            FunctionalCurrency = goc.FunctionalCurrency,
            ContractualCurrency = goc.ContractualCurrency,
            ValuationApproach = goc.ValuationApproach,
            OciType = goc.OciType,
            InitialProfitability = goc.Profitability,
            LiabilityType = goc.LiabilityType,
            AnnualCohort = goc.AnnualCohort,
            LineOfBusiness = goc.LineOfBusiness,
            IsReinsurance = goc is GroupOfReinsuranceContract,
            Partner = goc.Partner,
            EstimateType = iv.EstimateType,
            VariableType = iv.AocType,
            Novelty = iv.Novelty,
            AmountType = iv.AmountType,
            EconomicBasis = iv.EconomicBasis,
            AccidentYear = goc.LiabilityType == LiabilityTypes.LIC && iv.AccidentYear.HasValue
                ? iv.AccidentYear.Value
                : default,
            ServicePeriod = goc.LiabilityType == LiabilityTypes.LIC && iv.AccidentYear.HasValue
                ? iv.AccidentYear == args.Year ? ServicePeriod.CurrentService : ServicePeriod.PastService
                : ServicePeriod.NotApplicable,
            Projection = orderedProjectionConfigurations.ElementAtOrDefault(ind).SystemName,
            Value = val
        });


    public static async Task<ReportVariable[]> QueryReportVariablesSingleScenarioAsync(this IWorkspace workspace,
        (int Year, int Month, string ReportingNode, string Scenario) args,
        ProjectionConfiguration[] orderedProjectionConfigurations)
    {

        await workspace.Partition.SetAsync<PartitionByReportingNode>(new
            {ReportingNode = args.ReportingNode, Scenario = (string) null});
        await workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(new
            {ReportingNode = args.ReportingNode, Scenario = args.Scenario, Year = args.Year, Month = args.Month});
        var reportVariables = (await workspace.Query<GroupOfContract>()
                .Join(workspace.Query<IfrsVariable>(),
                    dn => dn.SystemName,
                    iv => iv.DataNode,
                    (dn, iv) => GetReportVariable(dn, iv, args, orderedProjectionConfigurations)
                )
                .ToArrayAsync())
            .SelectMany(rv => rv).ToArray();

        await workspace.Partition.SetAsync<PartitionByReportingNode>(null);
        await workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(null);
        return reportVariables;
    }


    public static async Task<ICollection<ReportVariable>> QueryReportVariablesAsync(this IWorkspace workspace,
        (int Year, int Month, string ReportingNode, string Scenario) args,
        ProjectionConfiguration[] orderedProjectionConfigurations)
    {
        var bestEstimate =
            (await workspace.QueryReportVariablesSingleScenarioAsync((args.Year, args.Month, args.ReportingNode, null),
                orderedProjectionConfigurations));
        return (args.Scenario == null)
            ? bestEstimate
            : (await workspace.QueryReportVariablesSingleScenarioAsync(
                (args.Year, args.Month, args.ReportingNode, args.Scenario), orderedProjectionConfigurations))
            .Union(bestEstimate.Select(x => x with {Scenario = args.Scenario}),
                Utils.EqualityComparer<ReportVariable>.Instance).ToArray();
    }


    public static string ParseReportingPeriodToDisplayString(int year, int periodOfYear, char separator) =>
        $"{year} {separator}{periodOfYear}";


    public static string ParseDimensionToDisplayString(string systemName, string displayName) =>
        $"{displayName} ({systemName})";


    public static async Task<(IDictionary<string, string>, IReadOnlyCollection<string>)> GetAutocompleteMappings<T>(
        this Systemorph.Vertex.DataSource.Api.IQuerySource querySource, bool hasNullAsFirstValue = default) where T : KeyedDimension
    {
        var query = await querySource.Query<T>().Select(x => new
                {x.SystemName, GuiName = ParseDimensionToDisplayString(x.SystemName, x.DisplayName), Order = 0})
            .ToArrayAsync(); //TODO extentions: populate order if type T is an orderedDimension. If it is a Hierarchical dimension then the order 
        var mappingDictionary =
            query.SelectMany(x => new[]
                    {new {GuiName = x.SystemName, x.SystemName}, new {GuiName = x.GuiName, x.SystemName}})
                .ToDictionary(x => x.GuiName, x => x.SystemName);
        var orderedDropDownValues = query.OrderBy(x => x.Order).ThenBy(x => x.GuiName).Select(x => x.GuiName);
        return (mappingDictionary,
            (hasNullAsFirstValue ? new string[] {null}.Concat(orderedDropDownValues) : orderedDropDownValues)
            .ToArray());
    }


    public record YieldCurveReport : KeyedRecord, IWithYearMonthAndScenario
    {
        [NotVisible]
        [Dimension(typeof(Currency))]
        public string Currency { get; init; }

        [NotVisible]
        [Dimension(typeof(int), nameof(Year))]
        public int Year { get; init; }

        [NotVisible]
        [Dimension(typeof(int), nameof(Month))]
        public int Month { get; init; }

        [NotVisible]
        [Dimension(typeof(Scenario))]
        public string Scenario { get; init; }

        [NotVisible]
        [Dimension(typeof(int), nameof(Index))]
        public int Index { get; init; }

        public double Value { get; init; }
    }


    public record RawVariableReport
    {
        [NotVisible]
        [Dimension(typeof(GroupOfContract))]
        public string DataNode { get; init; }

        [NotVisible]
        [AggregateBy]
        [Dimension(typeof(AocType))]
        public string AocType { get; init; }

        [NotVisible]
        [Dimension(typeof(Novelty))]
        public string Novelty { get; init; }

        [NotVisible]
        [AggregateBy]
        [Dimension(typeof(AmountType))]
        public string AmountType { get; init; }

        [NotVisible]
        [Dimension(typeof(EstimateType))]
        public string EstimateType { get; init; }

        [NotVisible]
        [Dimension(typeof(int), nameof(Index))]
        public int Index { get; init; }

        public double Value { get; init; }
    }


    public static IDataCube<YieldCurveReport> ToReportType(this YieldCurve[] yieldCurves)
        => yieldCurves.SelectMany(yc => yc.Values.Select((x, i) => new YieldCurveReport
            {
                Currency = yc.Currency, Year = yc.Year, Month = yc.Month, Scenario = yc.Scenario, Index = i, Value = x
            }))
            .ToDataCube();


    public static IDataCube<RawVariableReport> ToReportType(this IDataCube<RawVariable> rawVariable)
        => rawVariable.SelectMany(rv => rv.Values.Select((x, i) => new RawVariableReport
        {
            EstimateType = rv.EstimateType, AmountType = rv.AmountType, DataNode = rv.DataNode, AocType = rv.AocType,
            Novelty = rv.Novelty, Index = i, Value = x
        })).ToDataCube();
}



