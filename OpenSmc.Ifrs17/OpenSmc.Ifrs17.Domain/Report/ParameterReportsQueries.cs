using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Attributes.Arithmetics;
using Systemorph.Vertex.Workspace;


namespace OpenSmc.Ifrs17.Domain.Report;

public enum Period { Previous = -1, Current = 0 };

public abstract record ReportParameter {
    [Display(Order = -100)]
    [IdentityProperty]
    [NotAggregated]
    [Dimension(typeof(int), nameof(Year))]
    public int Year { get; init; }

    [Display(Order = -90)]
    [IdentityProperty]
    [NotAggregated]
    [Dimension(typeof(int), nameof(Month))]
    public int Month { get; init; } 

    [Display(Order = -80)]
    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }

    [IdentityProperty]
    [NotVisible]
    public Period Period { get; init; }

    [IdentityProperty]
    [NotVisible]
    [Dimension(typeof(GroupOfContract))]
    public string GroupOfContract { get; init; }
}


public record DataNodeStateReportParameter : ReportParameter  {
    public State State { get; init; }
}


public record YieldCurveReportParameter : ReportParameter  {
    [IdentityProperty]
    [NotVisible]
    public string YieldCurveType { get; init; }

    [Dimension(typeof(Currency))]
    public string Currency { get; init; }

    public string Name { get; init; }
}


public record SingleDataNodeReportParameter : ReportParameter  {

    public double PremiumAllocation { get; init; }

    [Dimension(typeof(CashFlowPeriodicity))]
    public CashFlowPeriodicity CashFlowPeriodicity { get; init; }
    
    [Dimension(typeof(InterpolationMethod))]
    public InterpolationMethod InterpolationMethod { get; init; }    
}


public record InterDataNodeReportParameter : ReportParameter  {
    [Dimension(typeof(GroupOfContract), nameof(LinkedDataNode))]
    public string LinkedDataNode { get; init; }

    public double ReinsuranceCoverage { get; init; }
}


public record PartnerRatingsReportParameter : ReportParameter  {
    [IdentityProperty]
    [NotAggregated]
    [Dimension(typeof(int), nameof(InitialYear))]
    [NotVisible]
    public int InitialYear { get; init; }

    [IdentityProperty]
    [NotVisible]
    public string PartnerRatingType { get; init; }

    [IdentityProperty]
    [NotVisible]
    [Dimension(typeof(Partner))]
    public string Partner { get; init; }

    [Dimension(typeof(CreditRiskRating))]
    public string CreditRiskRating { get; init; }
}
public record CreditDefaultRatesReportParameter : ReportParameter  {
    [IdentityProperty]
    [NotAggregated]
    [Dimension(typeof(int), nameof(InitialYear))]
    [NotVisible]
    public int InitialYear { get; init; }
    
    [IdentityProperty]
    [NotVisible]
    public string CreditDefaultRatesType { get; init; }

    [IdentityProperty]
    [Dimension(typeof(CreditRiskRating))]
    [NotVisible]
    public string CreditRiskRating { get; init; }
}

public static class ParameterReportQueriesExtensions
{
    public static async Task<DataNodeData[]> GetDataNodeDataReportParametersAsync(this IWorkspace workspace,
        ImportArgs args) =>
        (await workspace.LoadDataNodesAsync(args))
        .Values
        .ToArray();


    public static async Task<DataNodeStateReportParameter[]> GetDataNodeStateReportParametersAsync(
        this IWorkspace workspace, ImportArgs args) =>
        (await workspace.LoadCurrentAndPreviousParameterAsync<DataNodeState>(args, x => x.DataNode))
        .Values
        .SelectMany(x => x.Select(y =>
            new DataNodeStateReportParameter
            {
                GroupOfContract = y.Value.DataNode,
                Period = ((Period) y.Key),
                Year = y.Value.Year,
                Month = y.Value.Month,
                Scenario = y.Value.Scenario
            }
        ))
        .ToArray();


    public static async Task<YieldCurveReportParameter[]> GetYieldCurveReportParametersAsync(this IWorkspace workspace,
        ImportArgs args)
    {
        var dataNodeData = await workspace.GetDataNodeDataReportParametersAsync(args);

        var lockedYieldCurves = (await workspace.LoadLockedInYieldCurveAsync(args, dataNodeData))
            .Where(kvp => kvp.Value != null)
            .Select(x => new YieldCurveReportParameter
                {
                    GroupOfContract = x.Key,
                    YieldCurveType = "Locked-In Curve",
                    Year = x.Value.Year,
                    Month = x.Value.Month,
                    Scenario = x.Value.Scenario,
                    Currency = x.Value.Currency,
                    Name = x.Value.Name
                }
            )
            .ToArray();

        var currentYieldCurves = (await workspace.LoadCurrentYieldCurveAsync(args, dataNodeData))
            .Where(kvp => kvp.Value != null)
            .SelectMany(kvp => kvp.Value.Select(kvpInner => new YieldCurveReportParameter
            {
                GroupOfContract = kvp.Key,
                Period = (Period) kvpInner.Key,
                YieldCurveType = "CurrentCurve",
                Year = kvpInner.Value.Year,
                Month = kvpInner.Value.Month,
                Scenario = kvpInner.Value.Scenario,
                Currency = kvpInner.Value.Currency,
                Name = kvpInner.Value.Name
            }));

        return currentYieldCurves.Concat(lockedYieldCurves).ToArray();
    }


    public static async Task<SingleDataNodeReportParameter[]> GetSingleDataNodeReportParametersAsync(
        this IWorkspace workspace, ImportArgs args) =>
        (await workspace.LoadSingleDataNodeParametersAsync(args))
        .Values
        .SelectMany(x => x.Select(y => new SingleDataNodeReportParameter
            {
                GroupOfContract = y.Value.DataNode,
                Period = ((Period) y.Key),
                Year = y.Value.Year,
                Month = y.Value.Month,
                Scenario = y.Value.Scenario,
                PremiumAllocation = y.Value.PremiumAllocation,
                CashFlowPeriodicity = y.Value.CashFlowPeriodicity,
                InterpolationMethod = y.Value.InterpolationMethod
            }
        ))
        .ToArray();


    public static async Task<InterDataNodeReportParameter[]> GetInterDataNodeParametersAsync(this IWorkspace workspace,
        ImportArgs args) =>
        (await workspace.LoadInterDataNodeParametersAsync(args))
        .Values
        .SelectMany(x => x.SelectMany(y => y.Value.Select(z =>
            new InterDataNodeReportParameter
            {
                GroupOfContract = z.DataNode,
                Period = ((Period) y.Key),
                Year = z.Year,
                Month = z.Month,
                Scenario = z.Scenario,
                LinkedDataNode = z.LinkedDataNode,
                ReinsuranceCoverage = z.ReinsuranceCoverage
            }
        )))
        .Distinct() // Can be removed when we get rid of the dictionary
        .SelectMany(x => new[] {x, x with {GroupOfContract = x.LinkedDataNode, LinkedDataNode = x.GroupOfContract}}
        )
        .ToArray();


    public static async Task<PartnerRatingsReportParameter[]> GetCurrentPartnerRatingsReportParametersAsync(
        this IWorkspace workspace, ImportArgs args)
    {
        var currentPartnerRating =
            (await workspace.LoadCurrentAndPreviousParameterAsync<PartnerRating>(args, x => x.Partner))
            .Values
            .SelectMany(x => x.Select(y =>
                new PartnerRatingsReportParameter
                {
                    Period = ((Period) y.Key),
                    Partner = y.Value.Partner,
                    Year = y.Value.Year,
                    Month = y.Value.Month,
                    Scenario = y.Value.Scenario,
                    CreditRiskRating = y.Value.CreditRiskRating
                }
            ))
            .ToArray();

        return currentPartnerRating;
    }


    public static async Task<PartnerRatingsReportParameter[]> GetLockedInPartnerRatingsReportParametersAsync(
        this IWorkspace workspace, ImportArgs args)
    {
        var initialYears = (await workspace.LoadDataNodesAsync(args)).Values.Select(dn => dn.Year).ToHashSet();
        var lockedPartnerRating = Enumerable.Empty<PartnerRatingsReportParameter>();
        foreach (var y in initialYears)
        {
            var loadedPartnerRatingData = await workspace.LoadCurrentParameterAsync<PartnerRating>(
                args with {Year = y, Month = args.Year == y ? args.Month : Consts.MonthInAYear}, y => y.Partner);
            lockedPartnerRating = lockedPartnerRating.Concat(loadedPartnerRatingData.Select(x =>
                new PartnerRatingsReportParameter
                {
                    InitialYear = y,
                    PartnerRatingType = "Locked-In Rating",
                    Partner = x.Value.Partner,
                    Year = x.Value.Year,
                    Month = x.Value.Month,
                    Scenario = x.Value.Scenario,
                    CreditRiskRating = x.Value.CreditRiskRating
                }
            ));

        }

        return lockedPartnerRating.ToArray();
    }


    public static async Task<CreditDefaultRatesReportParameter[]> GetCurrentCreditDefaultRatesReportParametersAsync(
        this IWorkspace workspace, ImportArgs args)
    {
        var partnerRatings = await workspace.GetCurrentPartnerRatingsReportParametersAsync(args);

        var currentCreditDefaultRates =
            (await workspace.LoadCurrentAndPreviousParameterAsync<CreditDefaultRate>(args, x => x.CreditRiskRating))
            .Values
            .SelectMany(x => x.Select(y =>
                new CreditDefaultRatesReportParameter
                {
                    Period = ((Period) y.Key),
                    CreditRiskRating = y.Value.CreditRiskRating,
                    Year = y.Value.Year,
                    Month = y.Value.Month,
                    Scenario = y.Value.Scenario
                }
            )).ToArray();

        var partnerDefaultRates = partnerRatings.Join(
                currentCreditDefaultRates,
                pr => new {pr.Period, pr.CreditRiskRating},
                cdr => new {cdr.Period, cdr.CreditRiskRating},
                (pr, cdr) =>
                    new CreditDefaultRatesReportParameter
                    {
                        Period = pr.Period,
                        CreditRiskRating = pr.CreditRiskRating,
                        Year = cdr.Year,
                        Month = cdr.Month,
                        Scenario = cdr.Scenario,
                    }
            )
            .ToArray();

        return partnerDefaultRates;
    }


    public static async Task<CreditDefaultRatesReportParameter[]> GetLockedInCreditDefaultRatesReportParametersAsync(
        this IWorkspace workspace, ImportArgs args)
    {
        var initialYears = (await workspace.LoadDataNodesAsync(args)).Values.Select(dn => dn.Year).ToHashSet();
        var lockedCreditDefaultRate = Enumerable.Empty<CreditDefaultRatesReportParameter>();
        foreach (var y in initialYears)
        {
            var loadedCreditDefaultRateData = await workspace.LoadCurrentParameterAsync<CreditDefaultRate>(
                args with {Year = y, Month = args.Year == y ? args.Month : Consts.MonthInAYear},
                y => y.CreditRiskRating);
            lockedCreditDefaultRate = lockedCreditDefaultRate.Concat(loadedCreditDefaultRateData.Select(x =>
                new CreditDefaultRatesReportParameter
                {
                    InitialYear = y,
                    CreditDefaultRatesType = "Locked-In Rates",
                    CreditRiskRating = x.Value.CreditRiskRating,
                    Year = x.Value.Year,
                    Month = x.Value.Month,
                    Scenario = x.Value.Scenario
                }
            ));

        }

        return lockedCreditDefaultRate.ToArray();
    }

}


