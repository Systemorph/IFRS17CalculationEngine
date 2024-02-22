using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.DataStructures;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.DataNodeHub
{
    /*
    public async Task<ActivityLog> UploadDataNodesToWorkspaceAsync(IDataSet dataSet, IWorkspace workspace, IDataSource targetDataSource)
    {
        //workspace.Reset(x => x.ResetInitializationRules().ResetCurrentPartitions());
        //workspace.Initialize(x => x.FromSource(targetDataSource)
        //                           .DisableInitialization<RawVariable>()
        //                           .DisableInitialization<IfrsVariable>()
        //                           .DisableInitialization<DataNodeState>()
        //                           .DisableInitialization<DataNodeParameter>());

        //Activity.Start();
        //var args = await GetArgsAndCommitPartitionAsync<PartitionByReportingNode>(dataSet, targetDataSource);

        //if (Activity.HasErrors()) return Activity.Finish();

        var storage = new ParsingStorage(args, targetDataSource, workspace);
        await storage.InitializeAsync();
        if (Activity.HasErrors()) return Activity.Finish();

        var importLogPortfolios = await Import.FromDataSet(dataSet)
            .WithType<InsurancePortfolio>((dataset, datarow) => {
                var pf = new InsurancePortfolio
                {
                    SystemName = datarow.Field<string>(nameof(DataNode.SystemName)),
                    DisplayName = datarow.Field<string>(nameof(DataNode.DisplayName)),
                    Partition = storage.TargetPartitionByReportingNode.Id,
                    ContractualCurrency = datarow.Field<string>(nameof(DataNode.ContractualCurrency)),
                    FunctionalCurrency = storage.ReportingNode.Currency,
                    LineOfBusiness = datarow.Field<string>(nameof(DataNode.LineOfBusiness)),
                    ValuationApproach = datarow.Field<string>(nameof(DataNode.ValuationApproach)),
                    OciType = datarow.Field<string>(nameof(DataNode.OciType))
                };
                return ExtendPortfolio(pf, datarow);
            })
            .WithType<ReinsurancePortfolio>((dataset, datarow) => {
                var pf = new ReinsurancePortfolio
                {
                    SystemName = datarow.Field<string>(nameof(DataNode.SystemName)),
                    DisplayName = datarow.Field<string>(nameof(DataNode.DisplayName)),
                    Partition = storage.TargetPartitionByReportingNode.Id,
                    ContractualCurrency = datarow.Field<string>(nameof(DataNode.ContractualCurrency)),
                    FunctionalCurrency = storage.ReportingNode.Currency,
                    LineOfBusiness = datarow.Field<string>(nameof(DataNode.LineOfBusiness)),
                    ValuationApproach = datarow.Field<string>(nameof(DataNode.ValuationApproach)),
                    OciType = datarow.Field<string>(nameof(DataNode.OciType))
                };
                return ExtendPortfolio(pf, datarow);
            })
            .WithTarget(workspace)
            .ExecuteAsync();

        var portfolios = await workspace.Query<Portfolio>().ToDictionaryAsync(x => x.SystemName);
        var yieldCurveColumnGroupOfInsuranceContract = dataSet.Tables.Contains(nameof(GroupOfInsuranceContract)) && dataSet.Tables[nameof(GroupOfInsuranceContract)].Columns.Any(x => x.ColumnName == nameof(GroupOfInsuranceContract.YieldCurveName));
        var yieldCurveColumnGroupOfReinsuranceContract = dataSet.Tables.Contains(nameof(GroupOfReinsuranceContract)) && dataSet.Tables[nameof(GroupOfReinsuranceContract)].Columns.Any(x => x.ColumnName == nameof(GroupOfReinsuranceContract.YieldCurveName));

        var importLogGroupOfContracts = await Import.FromDataSet(dataSet)
            .WithType<GroupOfInsuranceContract>((dataset, datarow) => {
                var gicSystemName = datarow.Field<string>(nameof(DataNode.SystemName));
                var pf = datarow.Field<string>(nameof(InsurancePortfolio));
                if (!portfolios.TryGetValue(pf, out var portfolioData))
                {
                    ApplicationMessage.Log(Error.PortfolioGicNotFound, pf, gicSystemName);
                    return null;
                }
                var gic = new GroupOfInsuranceContract
                {
                    SystemName = gicSystemName,
                    DisplayName = datarow.Field<string>(nameof(DataNode.DisplayName)),
                    Partition = storage.TargetPartitionByReportingNode.Id,
                    ContractualCurrency = portfolioData.ContractualCurrency,
                    FunctionalCurrency = portfolioData.FunctionalCurrency,
                    LineOfBusiness = portfolioData.LineOfBusiness,
                    ValuationApproach = portfolioData.ValuationApproach,
                    OciType = portfolioData.OciType,
                    AnnualCohort = Convert.ToInt32(datarow.Field<object>(nameof(GroupOfContract.AnnualCohort))),
                    LiabilityType = datarow.Field<string>(nameof(GroupOfContract.LiabilityType)),
                    Profitability = datarow.Field<string>(nameof(GroupOfContract.Profitability)),
                    Portfolio = pf,
                    YieldCurveName = yieldCurveColumnGroupOfInsuranceContract
                                                                        ? datarow.Field<string>(nameof(GroupOfContract.YieldCurveName))
                                                                        : (string)null
                };
                return ExtendGroupOfContract(gic, datarow);
            })
            .WithType<GroupOfReinsuranceContract>((dataset, datarow) => {
                var gricSystemName = datarow.Field<string>(nameof(DataNode.SystemName));
                var pf = datarow.Field<string>(nameof(ReinsurancePortfolio));
                if (!portfolios.TryGetValue(pf, out var portfolioData))
                {
                    ApplicationMessage.Log(Error.PortfolioGicNotFound, pf, gricSystemName);
                    return null;
                }
                var gric = new GroupOfReinsuranceContract
                {
                    SystemName = gricSystemName,
                    DisplayName = datarow.Field<string>(nameof(DataNode.DisplayName)),
                    Partition = storage.TargetPartitionByReportingNode.Id,
                    ContractualCurrency = portfolioData.ContractualCurrency,
                    FunctionalCurrency = portfolioData.FunctionalCurrency,
                    LineOfBusiness = portfolioData.LineOfBusiness,
                    ValuationApproach = portfolioData.ValuationApproach,
                    OciType = portfolioData.OciType,
                    AnnualCohort = Convert.ToInt32(datarow.Field<object>(nameof(GroupOfContract.AnnualCohort))),
                    LiabilityType = datarow.Field<string>(nameof(GroupOfContract.LiabilityType)),
                    Profitability = datarow.Field<string>(nameof(GroupOfContract.Profitability)),
                    Portfolio = pf,
                    Partner = datarow.Field<string>(nameof(GroupOfContract.Partner)),
                    YieldCurveName = yieldCurveColumnGroupOfReinsuranceContract
                                                                            ? datarow.Field<string>(nameof(GroupOfContract.YieldCurveName))
                                                                            : (string)null
                };
                return ExtendGroupOfContract(gric, datarow);
            })
            .WithTarget(workspace)
            .ExecuteAsync();

        return Activity.Finish().Merge(importLogPortfolios).Merge(importLogGroupOfContracts);
    }
*/
}
