#!import "ReportScopes"


using Systemorph.Vertex.Pivot.Builder.Interfaces;
using Systemorph.InteractiveObjects;
using Systemorph.Vertex.Session;
using Systemorph.Vertex.Export;
using Systemorph.Vertex.Export.Factory;
using Systemorph.Vertex.InteractiveObjects;


public interface MutableScopeWithWorkspace<TStorage> : IMutableScopeWithStorage<TStorage> 
where TStorage : ReportStorage
{
    protected IWorkspace workspace => GetStorage().Workspace;   
}


public interface CurrencyFormsEntity : IMutableScope {
    [DropdownEnum(typeof(CurrencyType))]
    CurrencyType CurrencyType { get; set; } 
}


public interface ScenarioFormsEntity<TStorage> : MutableScopeWithWorkspace<TStorage> 
where TStorage : ReportStorage 
{
    [DropdownMethod(nameof(GetScenarioAutocompleteAsync))]
    [Display(Name = "Scenario")]
    string ScenarioControl { get; set; }

    [NotVisible] IDictionary<string, string> ScenarioMapping { get; set; }
    protected string Scenario => !string.IsNullOrWhiteSpace(ScenarioControl) && 
                                  ScenarioMapping is not null && 
                                  ScenarioMapping.TryGetValue(ScenarioControl, out var value) 
                                    ? value 
                                    : null;
    
    async Task<IReadOnlyCollection<string>> GetScenarioAutocompleteAsync(string userInput, int page, int pageSize) {
        (ScenarioMapping, var orderedDropDownValues) = await workspace.GetAutocompleteMappings<Scenario>(true);
        ScenarioMapping[Scenarios.Delta] = Scenarios.Delta; //TODO this behavior needs to be updated. PR#217
        ScenarioMapping[Scenarios.All] = Scenarios.All;
        return orderedDropDownValues.Concat(new string[] {Scenarios.Delta, Scenarios.All})
                                    .Where(x => userInput == null || x.Contains(userInput, StringComparison.OrdinalIgnoreCase))
                                    .ToArray(); 
    }
}


public interface ScenarioParameterFormsEntity<TStorage> : ScenarioFormsEntity<TStorage>
where TStorage : ReportStorage 
{
    async Task<IReadOnlyCollection<string>> GetScenarioAutocompleteAsync(string userInput, int page, int pageSize) {
        (ScenarioMapping, var orderedDropDownValues) = await workspace.GetAutocompleteMappings<Scenario>(true);
        return orderedDropDownValues.Where(x => userInput == null || x.Contains(userInput, StringComparison.OrdinalIgnoreCase))
                                    .ToArray(); 
    }
}


[InitializeScope(nameof(InitReportingNode))]
public interface ReportingNodeFormsEntity<TStorage> : MutableScopeWithWorkspace<TStorage> 
where TStorage : ReportStorage
{
    [DropdownMethod(nameof(GetReportingNodeAutocompleteAsync))]
    [Display(Name = "ReportingNode")]
    string ReportingNodeControl { get; set; }

    [NotVisible] IDictionary<string, string>  ReportingNodeMapping { get; set; }
    protected string ReportingNode => !string.IsNullOrWhiteSpace(ReportingNodeControl) && 
                                      ReportingNodeMapping is not null && 
                                      ReportingNodeMapping.TryGetValue(ReportingNodeControl, out var value)
                                        ? value
                                        : GetStorage().InitialReportingNode.SystemName; // Maybe these cases can be more specific

    async Task<IReadOnlyCollection<string>> GetReportingNodeAutocompleteAsync(string userInput, int page, int pageSize) {
        (ReportingNodeMapping, var orderedDropDownValues) = await workspace.GetAutocompleteMappings<ReportingNode>();
        return orderedDropDownValues.Where(x => userInput == null || x.Contains(userInput, StringComparison.OrdinalIgnoreCase)).ToArray(); 
    }

    void InitReportingNode() {
        ReportingNodeControl = ParseDimensionToDisplayString(GetStorage().InitialReportingNode.SystemName, GetStorage().InitialReportingNode.DisplayName);
    }
}


[InitializeScope(nameof(InitReportingPeriod))]
public interface MonthlyPeriodFormsEntity<TStorage> : MutableScopeWithWorkspace<TStorage> 
where TStorage : ReportStorage
{
    [DropdownMethod(nameof(GetReportingPeriodAutocompleteAsync))]
    string ReportingPeriod { get; set; }

    private char separator => 'M';
    private string[] ReportingPeriodSplit => ReportingPeriod.Split(separator);
    private int ParseReportingPeriod(int index) => !string.IsNullOrWhiteSpace(ReportingPeriod) && ReportingPeriodSplit is not null && Int32.TryParse(ReportingPeriodSplit.ElementAtOrDefault(index), out int value)
        ? value
        : default;

    protected int Year => ParseReportingPeriod(0);
    protected int Month => ParseReportingPeriod(1);

    async Task<IReadOnlyCollection<string>> GetReportingPeriodAutocompleteAsync(string userInput, int page, int pageSize) => 
        await workspace.Query<PartitionByReportingNodeAndPeriod>()
            .Where(x => x.Scenario == null)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Select(x => ParseReportingPeriodToDisplayString(x.Year, x.Month, separator))
            .Where(x => userInput == null || x.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .ToArrayAsync();

    void InitReportingPeriod() {
        ReportingPeriod = ParseReportingPeriodToDisplayString(GetStorage().InitialReportingPeriod.Year, GetStorage().InitialReportingPeriod.Month, separator);
    }
}


[InitializeScope(nameof(InitFilters))]
public interface BasicFilterFormsEntity<TStorage> : MutableScopeWithWorkspace<TStorage>
where TStorage : ReportStorage
{
    [DropdownMethod(nameof(GetFilterName))]
    string FilterName { get; set; }

    [DropdownMethod(nameof(GetBasicFilterAsync))]
    [Display(Name = "Filter Value")]
    string FilterValueControl { get; set; }

    [DropdownValues("", "Add", "Remove")]
    string FilterAction { get; set; }

    protected string[] defaultFilters => new string[] {
                        "",
                        nameof(ReportVariable.GroupOfContract),
                        nameof(ReportVariable.Portfolio),
                        nameof(ReportVariable.LineOfBusiness),
                        //nameof(ReportVariable.AnnualCohort),
                        nameof(ReportVariable.LiabilityType),
                        nameof(ReportVariable.ValuationApproach),
                        nameof(ReportVariable.OciType),
                        nameof(ReportVariable.InitialProfitability), //(\"Profitability\")
                        };

    [NotVisible] IReadOnlyCollection<string> specificFilters { get; set; }
    IReadOnlyCollection<string> GetFilterName() => defaultFilters.Union(specificFilters).ToArray();

    [NotVisible] IDictionary<string, IDictionary<string, string>> FilterMapping { get; set; }
    protected string FilterValue => !string.IsNullOrWhiteSpace(FilterName) && !string.IsNullOrWhiteSpace(FilterValueControl) && 
                                    FilterMapping is not null && FilterMapping.TryGetValue(FilterName, out var inner) &&
                                    inner.TryGetValue(FilterValueControl, out var value)
                                    ? value
                                    : null;

    async Task<IReadOnlyCollection<string>> GetFilterAutocompleteAsync<T>() where T : KeyedDimension {
        (var filterMapping, var orderedDropDownValues) = await workspace.GetAutocompleteMappings<T>();
        FilterMapping[typeof(T).Name] = filterMapping;
        return orderedDropDownValues;
    }

    async Task<IReadOnlyCollection<string>> GetBasicFilterAsync(string userInput, int page, int pageSize) =>
        new string[]{ null }.Concat(
            (FilterName switch
            {
                //GetAutocompleteMappings
               nameof(ReportVariable.Portfolio) => await GetFilterAutocompleteAsync<Portfolio>(),
               nameof(ReportVariable.GroupOfContract) => await GetFilterAutocompleteAsync<GroupOfContract>(),
               nameof(ReportVariable.LineOfBusiness) => await GetFilterAutocompleteAsync<LineOfBusiness>(),
               //nameof(ReportVariable.AnnualCohort) =>                                                  //TODO the filter is not applied because the prop is an Int
               nameof(ReportVariable.LiabilityType) => await GetFilterAutocompleteAsync<LiabilityType>(),
               nameof(ReportVariable.ValuationApproach) => await GetFilterAutocompleteAsync<ValuationApproach>(),
               nameof(ReportVariable.OciType) => await GetFilterAutocompleteAsync<OciType>(),
               nameof(ReportVariable.InitialProfitability) => await GetFilterAutocompleteAsync<Profitability>(),

               nameof(ReportVariable.Novelty) => await GetFilterAutocompleteAsync<Novelty>(),
               nameof(ReportVariable.VariableType) => await GetFilterAutocompleteAsync<VariableType>(),
               nameof(ReportVariable.EconomicBasis) => await GetFilterAutocompleteAsync<EconomicBasis>(),
               nameof(ReportVariable.AmountType) => await GetFilterAutocompleteAsync<AmountType>(),
               nameof(ReportVariable.EstimateType) => await GetFilterAutocompleteAsync<EstimateType>(),
                 _ => Enumerable.Empty<string>().ToArray()
            }).Where(x => userInput == null || x.Contains(userInput, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x)).ToArray();
        
    [NotVisible] IReadOnlyCollection<(string filterName, string filterValue)> InputDataFilter { get; set; }
    
    protected (string fileName, object filterValue)[] dataFilter => InputDataFilter.Select(x => (x.filterName, (object)x.filterValue)).ToArray(); //TODO this cast is needed by the Filter func

    IReadOnlyCollection<(string filterName, string filterValue)> GetFilters()
    {
        if(FilterAction == "Add")
            AddFilter(FilterName, FilterValue);
        if(FilterAction == "Remove")
            RemoveFilter(FilterName, FilterValue);
        return InputDataFilter;
    }

    private void AddFilter(string filterName, string filterValue)
    {
        if(!InputDataFilter.Contains((filterName, filterValue)))
            InputDataFilter = InputDataFilter.Append((filterName, filterValue)).ToArray();
    }
    
    private void RemoveFilter(string filterName, string filterValue)
    {
        if(InputDataFilter.Contains((filterName, filterValue)))
        {   var f = InputDataFilter.ToList();
            f.Remove((filterName, filterValue));
            InputDataFilter = f.ToArray();
        }
    }

    void InitFilters() {
        FilterMapping = new Dictionary<string, IDictionary<string,string>>() ;
        InputDataFilter = Enumerable.Empty<(string, string)>().ToArray();
    }
}


public interface BasicSliceAndDiceRowsFormsEntity<TStorage> : MutableScopeWithWorkspace<TStorage> 
where TStorage : ReportStorage
{
    [NotVisible] string SliceRowName { get; set; }

    protected IReadOnlyCollection<string> InputRowSlices => (SliceRowName is null ? Enumerable.Empty<string>() : SliceRowName.RepeatOnce()).ToArray();
    [NotVisible] IReadOnlyCollection<string> defaultRowSlices { get; set; }
    protected string[] rowSlices => defaultRowSlices.Union(InputRowSlices).ToArray();
}


public interface BasicSliceAndDiceColumnsFormsEntity<TStorage> : MutableScopeWithWorkspace<TStorage> 
where TStorage : ReportStorage
{
    [DropdownMethod(nameof(GetSliceColumnNameAutocomplete))]
    string SliceColumnName { get; set; }

    protected IReadOnlyCollection<string> InputColumnSlices => (SliceColumnName is null ? Enumerable.Empty<string>() : SliceColumnName.RepeatOnce()).ToArray();
    [NotVisible] IReadOnlyCollection<string> defaultColumnSlices { get; set; }
    protected string[] columnSlices => defaultColumnSlices.Union(InputColumnSlices).ToArray();

    IReadOnlyCollection<string> GetSliceColumnNameAutocomplete() => new [] {"", 
            nameof(ReportVariable.ReportingNode), 
            nameof(ReportVariable.Scenario),
            nameof(ReportVariable.Portfolio), 
            nameof(ReportVariable.GroupOfContract), 
            nameof(ReportVariable.LineOfBusiness),
            nameof(ReportVariable.LiabilityType),
            nameof(ReportVariable.InitialProfitability),
            nameof(ReportVariable.ValuationApproach),
            nameof(ReportVariable.AnnualCohort),
            nameof(ReportVariable.OciType),
            nameof(ReportVariable.IsReinsurance),
            nameof(ReportVariable.AccidentYear),
            
            nameof(ReportVariable.AmountType),
            nameof(ReportVariable.EstimateType),
            nameof(ReportVariable.EconomicBasis)};
}


public interface Data : IMutableScope<((int year, int month) reportingPeriod, string reportingNode, string scenario, CurrencyType currencyType, (string filterName, object filterValue)[] dataFilter)> {
    IDataCube<ReportVariable> InputDataCube { get; set; }
    IDataCube<ReportVariable> DataCube { get {
        if(InputDataCube is null) return Enumerable.Empty<ReportVariable>().ToDataCube();
        var filteredDataCube = (Identity.dataFilter is null || Identity.dataFilter.Length == 0) 
                                    ? InputDataCube 
                                    : InputDataCube.Filter(Identity.dataFilter); 
        switch (Identity.scenario)
        {
            case null : return filteredDataCube.Where(x => x.Scenario == null).ToDataCube();
            case Scenarios.All : return filteredDataCube.Select(x => x.Scenario == null ? x with {Scenario = Scenarios.Default } : x).ToDataCube();
            case Scenarios.Delta : 
                var bestEstimateById = filteredDataCube.Where(x => x.Scenario == null).ToDictionary(x => x.ToIdentityString());
                return filteredDataCube.Select(x => x.Scenario == null 
                                            ? x with { Scenario = Scenarios.Default } 
                                            : x with { Value = x.Value - (bestEstimateById.TryGetValue((x with {Scenario = null}).ToIdentityString(), out var be)? be.Value : 0.0) }).ToDataCube();               
            default : return filteredDataCube.Filter((nameof(ReportVariable.Scenario), Identity.scenario));
        }
    }}
} 


[InitializeScope(nameof(Init))]
public interface ReportScope : IMutableScope<string>, 
                            ReportingNodeFormsEntity<ReportStorage>, 
                            MonthlyPeriodFormsEntity<ReportStorage>, 
                            ScenarioFormsEntity<ReportStorage>, 
                            CurrencyFormsEntity, 
                            BasicSliceAndDiceRowsFormsEntity<ReportStorage>, 
                            BasicSliceAndDiceColumnsFormsEntity<ReportStorage>, 
                            BasicFilterFormsEntity<ReportStorage> {
    protected IPivotFactory report => GetStorage().Report;
    protected IExportVariable export => GetStorage().Export;
    protected int headerColumnWidthValue => 250;

    HashSet<(ReportIdentity, CurrencyType)> GetDataIdentities() => GetStorage().GetIdentities((Year, Month), ReportingNode, Scenario, CurrencyType); // TODO, add filter for identities, if the property is exposed at this level

    IDataCube<ReportVariable> GetData() => default;

    async Task<GridOptions> ToReportAsync() {
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var dataScope = GetScope<Data>(((Year, Month), ReportingNode, Scenario, CurrencyType, dataFilter));
        var dataCube = GetData();
        // This is a temporary solution to avoid an error from the empty report
        // Remove when the issue is solved on the platform - A.K.
        if (!dataCube.ToArray().Any()) return null;
        dataScope.InputDataCube = dataCube;
        return await GetReportTaskAsync(dataScope.DataCube);
    }

    // Using this routine is highly discouraged due to the mutlithreading issue -A.K.
    // Avoid using these methods if working with the DB -- it will trigger synchronization error in access to the DB
    async Task<ExportResult> ToCsvAsync(string fileName){
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var dataScope = GetScope<Data>(((Year, Month), ReportingNode, Scenario, CurrencyType, dataFilter));
        dataScope.InputDataCube = GetData();
        return await export.ToCsv(fileName)
                        .ForDataCube(dataScope.DataCube, config => config.WithQuerySource(workspace)
                                                                        .SliceRowsBy(rowSlices)
                                                                        .SliceColumnsBy(columnSlices))
                        .WithActivityLog()
                        .ExecuteAsync();
    }

    async Task<ExportResult> ToExcelAsync(string fileName){
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var dataScope = GetScope<Data>(((Year, Month), ReportingNode, Scenario, CurrencyType, dataFilter));
        dataScope.InputDataCube = GetData();
        return await export.ToExcel(fileName)
                        .ForDataCube(dataScope.DataCube, config => config.WithQuerySource(workspace) 
                                                                        .SliceRowsBy(rowSlices)
                                                                        .SliceColumnsBy(columnSlices))
                        .WithActivityLog()
                        .ExecuteAsync();
    }
    
    async Task<GridOptions> GetReportTaskAsync(IDataCube<ReportVariable> data) {
        return await report.ForDataCube(data)
            .WithQuerySource(workspace)
            .SliceRowsBy(rowSlices)
            .SliceColumnsBy(columnSlices)
            .ReportGridOptions(headerColumnWidth: headerColumnWidthValue)
            .ExecuteAsync();
    }

    void Init(){
        var task = InitReportStorageScopeAsync();
        task.Wait();
    }

    async Task InitReportStorageScopeAsync() { // This has the Async issue, but imo it should come in the future
        await GetStorage().InitializeReportIndependentCacheAsync();
    }
}


[InitializeScope(nameof(Init))]
public interface PvReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<LockedBestEstimate>(GetDataIdentities()).Aggregate().LockedBestEstimate + GetScopes<CurrentBestEstimate>(GetDataIdentities()).Aggregate().CurrentBestEstimate;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EconomicBasis) };
         // SpecificFiltersFormEntity
         specificFilters = new string[] {nameof(ReportVariable.AmountType)};
    }
}


[InitializeScope(nameof(Init))]
public interface RaReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<LockedRiskAdjustment>(GetDataIdentities()).Aggregate().LockedRiskAdjustment + GetScopes<CurrentRiskAdjustment>(GetDataIdentities()).Aggregate().CurrentRiskAdjustment;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EconomicBasis) };
    }
}


[InitializeScope(nameof(Init))]
public interface FcfReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() =>  GetScopes<Fcf>(GetDataIdentities()).Aggregate().Fcf;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EconomicBasis) };
    }
}


[InitializeScope(nameof(Init))]
public interface WrittenReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<WrittenAndAccruals>(GetDataIdentities()).Aggregate().Written;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.AmountType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency) };
    }
}


[InitializeScope(nameof(Init))]
public interface AccrualReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<WrittenAndAccruals>(GetDataIdentities()).Aggregate().Advance + GetScopes<WrittenAndAccruals>(GetDataIdentities()).Aggregate().Overdue;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         defaultRowSlices = new string[] { nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency) };
         // SpecificFiltersFormEntity
         specificFilters = new string[] {nameof(ReportVariable.AmountType)};
    }
}


[InitializeScope(nameof(Init))]
public interface DeferralReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<Deferrals>(GetDataIdentities()).Aggregate().Deferrals;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency) };
    }
}


[InitializeScope(nameof(Init))]
public interface ExpAdjReport: ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<ExperienceAdjustment>(GetDataIdentities()).Aggregate().ActuarialExperienceAdjustment;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         defaultRowSlices = new string[] { nameof(ReportVariable.AmountType), nameof(ReportVariable.EstimateType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency)};
         // SpecificFiltersFormEntity
         specificFilters = new string[] {nameof(ReportVariable.AmountType)};
    }
}


[InitializeScope(nameof(Init))]
public interface TmReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<LrcTechnicalMargin>(GetDataIdentities()).Aggregate().LrcTechnicalMargin;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency) };
    }
}


[InitializeScope(nameof(Init))]
public interface CsmReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<Csm>(GetDataIdentities()).Aggregate().Csm + GetScopes<Lc>(GetDataIdentities()).Aggregate().Lc + GetScopes<Loreco>(GetDataIdentities()).Aggregate().Loreco;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface ActLrcReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<LrcActuarial>(GetDataIdentities()).Aggregate().LrcActuarial;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface LrcReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() =>GetScopes<Lrc>(GetDataIdentities()).Aggregate().Lrc;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface ActLicReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<LicActuarial>(GetDataIdentities()).Aggregate().LicActuarial;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface LicReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<Lic>(GetDataIdentities()).Aggregate().Lic;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface FpReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<FinancialPerformance>(GetDataIdentities()).Aggregate().FinancialPerformance;

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.VariableType), nameof(ReportVariable.EstimateType)  };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency)};
    }
}


using Systemorph.Vertex.Pivot.Builder.Interfaces;
using Systemorph.InteractiveObjects;
using Systemorph.Vertex.Session;
public class Ifrs17Interactive {
    private IPivotFactory report;
    private IExportVariable export;
    private InteractiveObjectVariable interactiveObject;
    private ReportStorage storage;

    private IDictionary<string, Systemorph.Vertex.InteractiveObjects.InteractiveObjectView> interactiveObjectCache = new Dictionary<string, Systemorph.Vertex.InteractiveObjects.InteractiveObjectView>();

    public Ifrs17Interactive (IWorkspace workspace, 
                            IPivotFactory report, 
                            IExportVariable export,
                            InteractiveObjectVariable interactiveObject)
    {
        this.report = report;
        this.export = export;
        this.interactiveObject = interactiveObject;
        storage = new ReportStorage(workspace, report, export);
    }

    public void Reset(IWorkspace workspace) {
        storage = new ReportStorage(workspace, report, export);
        interactiveObjectCache = new Dictionary<string, Systemorph.Vertex.InteractiveObjects.InteractiveObjectView>();
    }

    public InteractiveObjectView GetFormsEntity<T>(string name = null) where T : ReportScope {
        var key = name ?? typeof(T).Name;
        if(!interactiveObjectCache.TryGetValue($"{key}FormsEntity", out var ret))
            ret = interactiveObjectCache[$"{key}FormsEntity"] = interactiveObject.CreateView($"{key}FormsEntity", _ => GetReportScope<T>(key));
        return ret;
    }

    public InteractiveObjectView GetReport<T>(string name = null) where T : ReportScope {
        var key = name ?? typeof(T).Name;
        if(!interactiveObjectCache.TryGetValue(key, out var ret)){
            ret = interactiveObjectCache[key] = interactiveObject.CreateView(key,
                _ => {
                    var scope = GetReportScope<T>(key);
                    var filters = scope.GetFilters(); // Not used and should be improved
                    return scope.ToReportAsync(); 
               }); 
        }
        return ret;
    }

    public async Task<ExportResult> ExportToCsvAsync<T>(string fileName, bool addDateTime = false)
    where T : ReportScope
    {
        var fullFileName = addDateTime ? AttachDateTime(fileName) : fileName; 
        var scope = GetReportScope<T>();
        var _ = scope.GetFilters();
        return await scope.ToCsvAsync(fullFileName);
    } 


    public async Task<ExportResult> ExportToExcelAsync<T>(string fileName, bool addDateTime = false)
    where T : ReportScope
    {
        var fullFileName = addDateTime ? AttachDateTime(fileName) : fileName;
        var scope = GetReportScope<T>();
        var _ = scope.GetFilters();
        return await scope.ToExcelAsync(fullFileName);
    }

    private string ToTwoDigitString(int number ) => number.ToString().Length == 1 ? "0" + number.ToString() : number.ToString();

    private string AttachDateTime(string fileName)
    {
        DateTime creationTime = DateTime.UtcNow;
        return fileName + "_" + creationTime.Year.ToString() + ToTwoDigitString(creationTime.Month) + ToTwoDigitString(creationTime.Day) +
                ToTwoDigitString(creationTime.Hour) + ToTwoDigitString(creationTime.Minute) + ToTwoDigitString(creationTime.Second);
    }

    // This routine is still buggy, triggering an infinite loop. -- A.K.
    // public InteractiveObjectView ToExcelInteractive<T>(string fileName, bool addDateTime = false, string name = null)
    // where T : ReportScope
    // {
    //     var key = name ?? typeof(T).Name;
    //     var fullFileName = addDateTime ? AttachDateTime(fileName) : fileName; 
    //     if (!interactiveObjectCache.TryGetValue($"{key}.xlsx", out var ret))
    //         ret = interactiveObjectCache[$"{key}.xlsx"] = interactiveObject.CreateView($"{key}.xlsx", _ => 
    //             {
    //                 var scope = GetReportScope<T>();
    //                 var filters = scope.GetFilters();
    //                 return scope.ToExcelAsync(fullFileName);
    //             });
    //     return ret;
    // }
    
    
    public ReportScope GetReportScope<T>(string name = null) where T : ReportScope => interactiveObject.State.GetScope<T>(name ?? typeof(T).Name, o => o.WithStorage(storage));

    // Keeping the old API
    public ReportScope PresentValues => GetReportScope<PvReport>();
    public ReportScope RiskAdjustments => GetReportScope<RaReport>();
    public ReportScope FulfillmentCashflows => GetReportScope<FcfReport>();
    public ReportScope WrittenActuals => GetReportScope<WrittenReport>();
    public ReportScope AccrualActuals => GetReportScope<AccrualReport>();
    public ReportScope DeferralActuals => GetReportScope<DeferralReport>();
    public ReportScope ExperienceAdjustments => GetReportScope<ExpAdjReport>();
    public ReportScope TechnicalMargins => GetReportScope<TmReport>();
    public ReportScope AllocatedTechnicalMargins => GetReportScope<CsmReport>();
    public ReportScope ActuarialLrc => GetReportScope<ActLrcReport>();
    public ReportScope Lrc => GetReportScope<LrcReport>();
    public ReportScope ActuarialLic => GetReportScope<ActLicReport>();
    public ReportScope Lic => GetReportScope<LicReport>();
    public ReportScope FinancialPerformance => GetReportScope<FpReport>();  
}



