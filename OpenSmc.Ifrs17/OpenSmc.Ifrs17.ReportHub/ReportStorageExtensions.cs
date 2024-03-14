using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.Constants.Enumerates;
using OpenSmc.Ifrs17.DataTypes.Constants.Validations;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Ifrs17.Utils;

namespace OpenSmc.Ifrs17.ReportHub;

public static class ReportStorageExtensions
{  
    public static Dictionary<string, Dictionary<FxPeriod, double>> GetExchangeRatesDictionary(this IWorkspace workspace, int year, int month)
        => (workspace.GetData<ExchangeRate>()
                .Where(x => x.Year == year - 1 && x.Month == BusinessConstant.MonthInAYear && x.FxType == FxType.Spot ||
                            x.Year == year && x.Month == month)
                .ToArray())
            .ToDictionaryGrouped(x => x.Currency,
                x => x.ToDictionary(y => (y.Year, y.Month, y.FxType) switch
                    {
                        (_, _, _) when y.Year == year - 1 && y.Month == BusinessConstant.MonthInAYear && y.FxType == FxType.Spot => FxPeriod.BeginningOfPeriod,
                        (_, _, _) when y.Year == year && y.Month == month && y.FxType == FxType.Average => FxPeriod.Average,
                        (_, _, _) when y.Year == year && y.Month == month && y.FxType == FxType.Spot => FxPeriod.EndOfPeriod
                    },
                    y => y.FxToGroupCurrency));

    public static ICollection<ReportVariable> QueryReportVariables(this IWorkspace workspace, (int Year, int Month, string ReportingNode, string Scenario) args, ProjectionConfiguration[] orderedProjectionConfigurations)
    {
        var bestEstimate = workspace.QueryReportVariablesSingleScenario((args.Year, args.Month, args.ReportingNode, null), orderedProjectionConfigurations);
        return (args.Scenario == null)
            ? bestEstimate
            : workspace.QueryReportVariablesSingleScenario((args.Year, args.Month, args.ReportingNode, args.Scenario), orderedProjectionConfigurations)
            .Union(bestEstimate.Select(x => x with { Scenario = args.Scenario }), Utils.EqualityComparer<ReportVariable>.Instance).ToArray();
    }

    public static ReportVariable[] QueryReportVariablesSingleScenario(this IWorkspace workspace, (int Year, int Month, string ReportingNode, string Scenario) args,
        ProjectionConfiguration[] orderedProjectionConfigurations)
    {
        //await workspace.Partition.SetAsync<PartitionByReportingNode>(new { ReportingNode = args.ReportingNode, Scenario = (string)null });
        //await workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(new { ReportingNode = args.ReportingNode, Scenario = args.Scenario, Year = args.Year, Month = args.Month });
        var reportVariables = workspace.GetData<GroupOfContract>()
                .Join(workspace.GetData<IfrsVariable>(),
                    dn => dn.SystemName,
                    iv => iv.DataNode,
                    (dn, iv) => GetReportVariable(dn, iv, args, orderedProjectionConfigurations)
                )
                .ToArray()
            .SelectMany(rv => rv).ToArray();

        //await workspace.Partition.SetAsync<PartitionByReportingNode>(null);
        //await workspace.Partition.SetAsync<PartitionByReportingNodeAndPeriod>(null);
        return reportVariables;
    }

    public static IEnumerable<ReportVariable> GetReportVariable(GroupOfContract goc, IfrsVariable iv, (int Year, int Month, string ReportingNode, string Scenario) args, ProjectionConfiguration[] orderedProjectionConfigurations) 
        => iv.Values.Select((val, ind) => new ReportVariable
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

    public static double GetCurrencyToGroupFx(Dictionary<string, Dictionary<FxPeriod, double>> exchangeRates, string currency, FxPeriod fxPeriod, string groupCurrency)
    {
        if(currency == groupCurrency)
          return 1;
    
        if (!exchangeRates.TryGetValue(currency, out var currencyToGroup))
            throw new Exception(Error.ExchangeRateCurrency.GetMessage(currency));
        if (!currencyToGroup.TryGetValue(fxPeriod, out var currencyToGroupFx))
            throw new Exception(Error.ExchangeRateNotFound.GetMessage(currency, fxPeriod.ToString()));
        return currencyToGroupFx;
    }
}