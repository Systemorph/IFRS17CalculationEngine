

using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Export;


public static IDocumentBuilder MainTabConfiguration<T>(this IDocumentBuilder builder, T args) where T : IfrsPartition
    => builder.WithTable<T>( config => config .AtBeginning() 
        .WithName(Main) 
        .WithSource(source => args.RepeatOnce().AsQueryable()) 
        .WithColumn(x => x.Id, x => x.Delete()));


public static IDocumentBuilder PortfolioConfiguration<T>(this IDocumentBuilder builder, Type DependsOnType = default) where T : Portfolio
    => builder.WithTable<T>(config => { 
        if(DependsOnType != default)
            config = config.DependsOn(DependsOnType);               
        return config .AtBeginning() 
            .WithColumn(x => x.DisplayName, x => x.AtBeginning())
            .WithColumn(x => x.SystemName, x => x.AtBeginning())
            .WithColumn(x => x.Partition, x => x.Delete())
            .WithColumn(x => x.FunctionalCurrency, x => x.Delete());
    });


public static IDocumentBuilder GroupofContractConfiguration<T>(this IDocumentBuilder builder, Type DependsOnType = default) where T : GroupOfContract
    => builder.WithTable<T>(config => { 
        if(DependsOnType != default)
            config = config.DependsOn(DependsOnType);      
        if(typeof(T).Name == nameof(GroupOfInsuranceContract))
            config = config.WithColumn(x => x.Partner, x => x.Delete());
        return config .AtBeginning() 
            .WithColumn(x => x.DisplayName, x => x.AtBeginning())
            .WithColumn(x => x.SystemName, x => x.AtBeginning())
            .WithColumn(x => x.Partition, x => x.Delete())
            .WithColumn(x => x.ContractualCurrency, x => x.Delete())
            .WithColumn(x => x.FunctionalCurrency, x => x.Delete())
            .WithColumn(x => x.LineOfBusiness, x => x.Delete())
            .WithColumn(x => x.OciType, x => x.Delete())
            .WithColumn(x => x.ValuationApproach, x => x.Delete());
    });


using DocumentFormat.OpenXml.Spreadsheet;


public record HelperState { public string State {get; init;} }


public static IExcelDocumentBuilder DataNodeStateConfiguration (this IExcelDocumentBuilder builder, DataNodeState[] data)
    => builder
        .WithTable<LiabilityType>(x => x.Delete())
        .WithTable<Profitability>(x => x.Delete())
        .WithTable<Portfolio>(x => x.Delete())
        .WithTable<Currency>(x => x.Delete())
        .WithTable<LineOfBusiness>(x => x.Delete())
        .WithTable<ValuationApproach>(x => x.Delete())
        .WithTable<OciType>(x => x.Delete())
        .WithTable<Partner>(x => x.Delete())
        .WithTable<ReportingNode>(x => x.Delete())
        .WithTable<Scenario>(x => x.Delete())
        .WithTable<DataNodeState>(config => config       
            .AtBeginning() 
            .WithSource(source => data.AsQueryable())
            .WithColumn(x => x.Partition, x => x.Delete())
            .WithColumn(x => x.Month, x => x.Delete())
            .WithColumn(x => x.Year, x => x.Delete())
            .WithColumn(x => x.Id, x => x.Delete())
            .WithColumn(x => x.Scenario, x => x.Delete())
            .WithColumn(x => x.State, y => y.WithDataValidation(z => z.WithReferenceTo<HelperState, string>(t => t.State)))
        );


public static IExcelDocumentBuilder StateEnumConfiguration (this IExcelDocumentBuilder builder)
{
    var helperState = new[] {new HelperState {State = "Active"}, new HelperState {State = "Inactive"} }; 
    return builder.WithTable<HelperState>( config => config .WithSheetVisibility(SheetStateValues.Hidden)
                //.WithColumn(x => x.State, z => z.WithNamedRange(y => y.WithName("HelperState_State")))
                .WithColumn(x => x.State, z => z.WithDefaultNamedRange())
                .WithSource(source => helperState.AsQueryable()) );
}


public static IExcelDocumentBuilder DataNodeParameterConfiguration (this IExcelDocumentBuilder builder, Dictionary<string, DataNodeParameter[]> data)
    => builder
        .WithTable<LiabilityType>(x => x.Delete())
        .WithTable<Profitability>(x => x.Delete())
        .WithTable<Portfolio>(x => x.Delete())
        .WithTable<Currency>(x => x.Delete())
        .WithTable<LineOfBusiness>(x => x.Delete())
        .WithTable<ValuationApproach>(x => x.Delete())
        .WithTable<OciType>(x => x.Delete())
        .WithTable<Partner>(x => x.Delete())
        .WithTable<ReportingNode>(x => x.Delete())
        .WithTable<Scenario>(x => x.Delete())
        .WithTable<InterDataNodeParameter>(config => config       
            .AtBeginning() 
            .WithSource(source => data[nameof(InterDataNodeParameter)].Cast<InterDataNodeParameter>().AsQueryable())
            .WithColumn(x => x.Partition, x => x.Delete())
            .WithColumn(x => x.Month, x => x.Delete())
            .WithColumn(x => x.Id, x => x.Delete())
            .WithColumn(x => x.Scenario, x => x.Delete())
            .WithColumn(x => x.Year, x => x.Delete())
        )
        .WithTable<SingleDataNodeParameter>(config => config       
            .AtBeginning() 
            .WithSource(source => data[nameof(SingleDataNodeParameter)].Cast<SingleDataNodeParameter>().AsQueryable())
            .WithColumn(x => x.DataNode, x => x.WithHeader("DataNode"))
            .WithColumn(x => x.Partition, x => x.Delete())
            .WithColumn(x => x.Month, x => x.Delete())
            .WithColumn(x => x.Year, x => x.Delete())
            .WithColumn(x => x.Year, x => x.Delete())
            .WithColumn(x => x.Id, x => x.Delete())
            .WithColumn(x => x.Scenario, x => x.Delete())
        );



