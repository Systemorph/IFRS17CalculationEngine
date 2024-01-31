using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel.Args;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes;
using OpenSmc.Scopes;
using OpenSmc.Workspace;
using Systemorph.InteractiveObjects;
using Systemorph.Vertex.Export.Factory;
using Systemorph.Vertex.Grid.Model;
using Systemorph.Vertex.InteractiveObjects;
using Systemorph.Vertex.InteractiveObjects.Dropdown;
using Systemorph.Vertex.Pivot.Builder.Interfaces;
using Systemorph.Vertex.Pivot.Reporting;

namespace OpenSmc.Ifrs17.Domain.Report;
public interface ParameterReportFormsEntityInteractive<TStorage> : MutableScopeWithWorkspace<TStorage>
where TStorage : ReportStorage
{
    [DropdownMethod(nameof(GetParameterReportTypes))]
    string ReportType{get; set;}

    string[] GetParameterReportTypes() => typeof(ParameterReportType).GetAllPublicConstantValues<string>();
}


[InitializeScope(nameof(Init))]
public interface ParameterReportScopeInteractive: IMutableScope<string>, 
                                    ReportingNodeFormsEntity<ReportStorage>, 
                                    MonthlyPeriodFormsEntity<ReportStorage>, 
                                    ScenarioParameterFormsEntity<ReportStorage>,
                                    CurrencyFormsEntity, 
                                    ParameterReportFormsEntityInteractive<ReportStorage>
{
    protected IPivotFactory report => GetStorage().Report;
    protected IExportVariable export => GetStorage().Export;
    protected int headerColumnWidthValue => 250;

    HashSet<(ReportIdentity, CurrencyType)> GetDataIdentities() => GetStorage().GetIdentities((Year, Month), ReportingNode, Scenario, CurrencyType); 


    ImportArgs GetArgs() => new ImportArgs(ReportingNode, Year, Month, default, Scenario, default); 

    async Task<GridOptions> GetDataNodeReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetDataNodeDataReportParametersAsync(GetArgs());
        return await report.ForObjects(data)
                            //.WithQuerySource(workspace)
                            .GroupRowsBy(x => x.Portfolio)
                            .GroupRowsBy(x => x.DataNode)
                            .ToTable()
                            .ExecuteAsync();
    }

    async Task<GridOptions> GetDataNodeStatesReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetDataNodeStateReportParametersAsync(GetArgs());
        return await report.ForObjects(data)
                            //.WithQuerySource(workspace)
                            .GroupRowsBy(x => x.GroupOfContract)
                            .GroupColumnsBy(x => x.Period)
                            .ToTable()
                            .ExecuteAsync();
    }

    async Task<GridOptions> GetYieldCurvesReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetYieldCurveReportParametersAsync(GetArgs());
        return await report.ForObjects(data)
                        //.WithQuerySource(workspace)
                        .GroupRowsBy(x => x.GroupOfContract)
                        .GroupColumnsBy(x => x.YieldCurveType)
                        .GroupColumnsBy(x => x.Period)
                        .ToTable()
                        .ExecuteAsync();
    }

    async Task<GridOptions> GetSingleDataNodeReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetSingleDataNodeReportParametersAsync(GetArgs());
        return await report.ForObjects(data)
                        //.WithQuerySource(workspace)
                        .GroupRowsBy(x => x.GroupOfContract)
                        .GroupColumnsBy(x => x.Period)
                        .ToTable()
                        .ExecuteAsync();
    }

    async Task<GridOptions> GetInterDataNodeParametersReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetInterDataNodeParametersAsync(GetArgs());
        return await report.ForObjects(data)
                        //.WithQuerySource(workspace)
                        .GroupRowsBy(x => x.GroupOfContract)
                        .GroupRowsBy(x => x.LinkedDataNode)
                        .GroupColumnsBy(x => x.Period)
                        .ToTable()
                        .ExecuteAsync();
    }

    async Task<GridOptions> GetCurrentPartnerRatingReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetCurrentPartnerRatingsReportParametersAsync(GetArgs());
        return await report.ForObjects(data)
                        //.WithQuerySource(workspace)
                        .GroupRowsBy(x => x.Partner)
                        .GroupColumnsBy(x => x.Period)
                        .ToTable()
                        .ExecuteAsync();
    }

    async Task<GridOptions> GetCurrentDefaultRatesReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetCurrentCreditDefaultRatesReportParametersAsync(GetArgs());
        return await report.ForObjects(data)
                        //.WithQuerySource(workspace)
                        .GroupRowsBy(x => x.CreditRiskRating)
                        .GroupColumnsBy(x => x.Period)
                        .ToTable()
                        .ExecuteAsync();
    }

    async Task<GridOptions> GetLockedInPartnerRatingReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetLockedInPartnerRatingsReportParametersAsync(GetArgs());
        return await report.ForObjects(data)
                        //.WithQuerySource(workspace)
                        .GroupRowsBy(x => x.Partner)
                        .GroupColumnsBy(x => x.PartnerRatingType)
                        .GroupColumnsBy(x => "Initial Year: " + x.InitialYear)
                        .ToTable()
                        .ExecuteAsync();
    }

    async Task<GridOptions> GetLockedInDefaultRatesReport()
    {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var data = await workspace.GetLockedInCreditDefaultRatesReportParametersAsync(GetArgs());
        return await report.ForObjects(data)
                        //.WithQuerySource(workspace)
                        .GroupRowsBy(x => x.CreditRiskRating)
                        .GroupColumnsBy(x => x.CreditDefaultRatesType)
                        .GroupColumnsBy(x => "Initial Year: " + x.InitialYear)
                        .ToTable()
                        .ExecuteAsync();
    }

    async Task<GridOptions> ToReport() => ReportType switch
        {
            ParameterReportType.DataNode => await GetDataNodeReport(), 
            ParameterReportType.DataNodeState => await GetDataNodeStatesReport(), 
            ParameterReportType.YieldCurves => await GetYieldCurvesReport(), 
            ParameterReportType.SingleDataNodeParameters => await GetSingleDataNodeReport(), 
            ParameterReportType.InterDataNodeParameters => await GetInterDataNodeParametersReport(), 
            ParameterReportType.CurrentPartnerRating => await GetCurrentPartnerRatingReport(), 
            ParameterReportType.CurrentPartnerDefaultRates => await GetCurrentDefaultRatesReport(),
            ParameterReportType.LockedInPartnerRating => await GetLockedInPartnerRatingReport(), 
            ParameterReportType.LockedInPartnerDefaultRates => await GetLockedInDefaultRatesReport(),
            _ => null
        };

    void Init(){
        var task = InitReportStorageScopeAsync();
        task.Wait();
    }

    async Task InitReportStorageScopeAsync() { 
       await GetStorage().InitializeReportIndependentCacheAsync();
    }
}


class InteractiveParameterReportsFull
{
    private IPivotFactory report;
    private IExportVariable export;
    private InteractiveObjectVariable interactiveObject;
    public ReportStorage storage;

    private IDictionary<string, InteractiveObjectView> interactiveObjectCache = new Dictionary<string, InteractiveObjectView>();
    
    public InteractiveParameterReportsFull(IWorkspace workspace, 
                                    IPivotFactory report, 
                                    IExportVariable export, 
                                    InteractiveObjectVariable interactiveObject)
    {
        this.report = report;
        this.export = export;
        this.interactiveObject = interactiveObject;
        this.storage = new ReportStorage(workspace, report, export);
    }

    public void Reset(IWorkspace workspace)
    {
        storage = new ReportStorage(workspace, report, export);
        interactiveObjectCache = new Dictionary<string, InteractiveObjectView>() {};
    }

    public ParameterReportScopeInteractive GetReportScope<T>(string name = null) 
        where T : ParameterReportScopeInteractive => 
        interactiveObject.State.GetScope<T>(name ?? typeof(T).Name, o => o.WithStorage(storage));

    public InteractiveObjectView GetFormsEntity<T>(string name = null)
    where T : ParameterReportScopeInteractive
    {
        var key = name ?? typeof(T).Name;
        if (!interactiveObjectCache.TryGetValue($"{key}FormsEntity", out var ret))
            ret = interactiveObjectCache[$"{key}FormsEntity"] = interactiveObject.CreateView($"{key}FormsEntity", 
                                                                    _ => GetReportScope<T>(name : "ParameterReports"));
        return ret;
    }

    public InteractiveObjectView GetReport<T>(string name = null)
    where T : ParameterReportScopeInteractive
    {
        var key = name ?? typeof(T).Name;
        if (!interactiveObjectCache.TryGetValue(key, out var ret))
            ret = interactiveObjectCache[key] = interactiveObject.CreateView(key, 
                                                    _ => GetReportScope<T>(name : "ParameterReports").ToReport());
        return ret;
    }

}



