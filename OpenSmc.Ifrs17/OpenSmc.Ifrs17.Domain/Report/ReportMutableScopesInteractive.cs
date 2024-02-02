using System.ComponentModel.DataAnnotations;
using OpenSmc.Collections;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Pivot.Builder.Interfaces;
using Systemorph.InteractiveObjects;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Export;
using Systemorph.Vertex.Export.Csv.Pivot;
using Systemorph.Vertex.Export.Excel.Pivot;
using Systemorph.Vertex.Export.Factory;
using Systemorph.Vertex.Grid.Model;
using Systemorph.Vertex.InteractiveObjects;
using Systemorph.Vertex.InteractiveObjects.Dropdown;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;
using OpenSmc.Scopes;
using OpenSmc.Workspace;
using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;

// TODO : Check if this is no entirely obsolete - A.K.

namespace OpenSmc.Ifrs17.Domain.Report;

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
        ReportingNodeControl = ReportConfigExtensions.ParseDimensionToDisplayString(GetStorage().InitialReportingNode.SystemName, GetStorage().InitialReportingNode.DisplayName);
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
            .Select(x => ReportConfigExtensions.ParseReportingPeriodToDisplayString(x.Year, x.Month, separator))
            .Where(x => userInput == null || x.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .ToArrayAsync();

    void InitReportingPeriod() {
        ReportingPeriod = ReportConfigExtensions.ParseReportingPeriodToDisplayString(GetStorage().InitialReportingPeriod.Year, GetStorage().InitialReportingPeriod.Month, separator);
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


public interface IData : IMutableScope<((int year, int month) reportingPeriod, string reportingNode, string scenario, CurrencyType currencyType, (string filterName, object filterValue)[] dataFilter)> {
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
        var dataScope = GetScope<IData>(((Year, Month), ReportingNode, Scenario, CurrencyType, dataFilter));
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
        var dataScope = GetScope<IData>(((Year, Month), ReportingNode, Scenario, CurrencyType, dataFilter));
        dataScope.InputDataCube = GetData();
        return await export.ToCsv(fileName)
                        .ForDataCube(dataScope.DataCube, config => config//.WithQuerySource(workspace)
                                                                        .SliceRowsBy(rowSlices)
                                                                        .SliceColumnsBy(columnSlices))
                        .WithActivityLog()
                        .ExecuteAsync();
    }

    async Task<ExportResult> ToExcelAsync(string fileName){
        await GetStorage().InitializeAsync((Year, Month), ReportingNode, Scenario, CurrencyType);
        var dataScope = GetScope<IData>(((Year, Month), ReportingNode, Scenario, CurrencyType, dataFilter));
        dataScope.InputDataCube = GetData();
        return await export.ToExcel(fileName)
                        .ForDataCube(dataScope.DataCube, config => config//.WithQuerySource(workspace) 
                                                                        .SliceRowsBy(rowSlices)
                                                                        .SliceColumnsBy(columnSlices))
                        .WithActivityLog()
                        .ExecuteAsync();
    }
    
    async Task<GridOptions> GetReportTaskAsync(IDataCube<ReportVariable> data) {
        return await report.ForDataCube(data)
            //.WithQuerySource(workspace)
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
public interface IPvReport : ReportScope {

    IDataCube<ReportVariable> ReportScope.GetData()
    {
        return GetScopes<ILockedBestEstimate>(GetDataIdentities())
                   .Select(x => x.LockedBestEstimate)
                   .Aggregate() +
               GetScopes<ICurrentBestEstimate>(GetDataIdentities())
                   .Select(x => x.CurrentBestEstimate)
                   .Aggregate();
    }

    void Init() {
         // BasicSliceAndDiceFormsEntity
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EconomicBasis) };
         // SpecificFiltersFormEntity
         specificFilters = new string[] {nameof(ReportVariable.AmountType)};
    }
}


[InitializeScope(nameof(Init))]
public interface IRaReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<ILockedRiskAdjustment>(GetDataIdentities())
        .Select(x => x.LockedRiskAdjustment)
        .Aggregate() + GetScopes<ICurrentRiskAdjustment>(GetDataIdentities())
        .Select(x => x.CurrentRiskAdjustment)
        .Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EconomicBasis) };
    }
}


[InitializeScope(nameof(Init))]
public interface IFcfReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() =>  GetScopes<IFcf>(GetDataIdentities()).Select(x => x.Fcf).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EconomicBasis) };
    }
}


[InitializeScope(nameof(Init))]
public interface IWrittenReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<IWrittenAndAccruals>(GetDataIdentities())
        .Select(x => x.Written).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.AmountType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency) };
    }
}


[InitializeScope(nameof(Init))]
public interface IAccrualReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<IWrittenAndAccruals>(GetDataIdentities()).Select(x => x.Advance).Aggregate() + GetScopes<IWrittenAndAccruals>(GetDataIdentities()).Select(x => x.Overdue).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         defaultRowSlices = new string[] { nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency) };
         // SpecificFiltersFormEntity
         specificFilters = new string[] {nameof(ReportVariable.AmountType)};
    }
}


[InitializeScope(nameof(Init))]
public interface IDeferralReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<IDeferrals>(GetDataIdentities()).Select(x => x.Deferrals).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency) };
    }
}


[InitializeScope(nameof(Init))]
public interface IExpAdjReport: ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<IExperienceAdjustment>(GetDataIdentities()).Select(x => x.ActuarialExperienceAdjustment).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         defaultRowSlices = new string[] { nameof(ReportVariable.AmountType), nameof(ReportVariable.EstimateType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency)};
         // SpecificFiltersFormEntity
         specificFilters = new string[] {nameof(ReportVariable.AmountType)};
    }
}


[InitializeScope(nameof(Init))]
public interface ITmReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<ILrcTechnicalMargin>(GetDataIdentities()).Select(x => x.LrcTechnicalMargin).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency) };
    }
}


[InitializeScope(nameof(Init))]
public interface ICsmReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<ICsm>(GetDataIdentities()).Select(x => x.Csm).Aggregate() + 
                                                       GetScopes<ILc>(GetDataIdentities()).Select(x => x.Lc).Aggregate() + 
                                                       GetScopes<ILoreco>(GetDataIdentities()).Select(x => x.Loreco).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface IActLrcReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<ILrcActuarial>(GetDataIdentities()).Select(x => x.LrcActuarial).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface ILrcReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() =>GetScopes<ILrc>(GetDataIdentities()).Select(x => x.Lrc).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface IActLicReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<ILicActuarial>(GetDataIdentities()).Select(x => x.LicActuarial).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.Novelty), nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface ILicReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<ILic>(GetDataIdentities()).Select(x => x.Lic).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.VariableType) };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency), nameof(ReportVariable.EstimateType) };
    }
}


[InitializeScope(nameof(Init))]
public interface IFpReport : ReportScope {
    
    IDataCube<ReportVariable> ReportScope.GetData() => GetScopes<IFinancialPerformance>(GetDataIdentities()).Select(x => x.FinancialPerformance).Aggregate();

     void Init() {
         // BasicSliceAndDiceFormsEntity
         specificFilters = Enumerable.Empty<string>().ToArray();
         defaultRowSlices = new string[] { nameof(ReportVariable.VariableType), nameof(ReportVariable.EstimateType)  };
         defaultColumnSlices = new string[] { nameof(ReportVariable.Currency)};
    }
}


//using Systemorph.Vertex.Pivot.Builder.Interfaces;
//using Systemorph.InteractiveObjects;
//using Systemorph.Vertex.Session;
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
    public ReportScope PresentValues => GetReportScope<IPvReport>();
    public ReportScope RiskAdjustments => GetReportScope<IRaReport>();
    public ReportScope FulfillmentCashflows => GetReportScope<IFcfReport>();
    public ReportScope WrittenActuals => GetReportScope<IWrittenReport>();
    public ReportScope AccrualActuals => GetReportScope<IAccrualReport>();
    public ReportScope DeferralActuals => GetReportScope<IDeferralReport>();
    public ReportScope ExperienceAdjustments => GetReportScope<IExpAdjReport>();
    public ReportScope TechnicalMargins => GetReportScope<ITmReport>();
    public ReportScope AllocatedTechnicalMargins => GetReportScope<ICsmReport>();
    public ReportScope ActuarialLrc => GetReportScope<IActLrcReport>();
    public ReportScope Lrc => GetReportScope<ILrcReport>();
    public ReportScope ActuarialLic => GetReportScope<IActLicReport>();
    public ReportScope Lic => GetReportScope<ILicReport>();
    public ReportScope FinancialPerformance => GetReportScope<IFpReport>();  
}



