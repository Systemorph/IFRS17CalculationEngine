using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ParameterDataHub;

public static class ParameterHubConfiguration
{
    //Configuration 1: Use a dictionary to initialize the DataHub 
    public static MessageHubConfiguration ConfigureParameterDataDictInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc.FromConfigurableDataSource("ParameterDataSource", ds => ds
                .WithType<ExchangeRate>(t => t.WithKey(x => (x.Year, x.Month, x.Scenario, x.FxType, x.Currency)).WithInitialData((IEnumerable<ExchangeRate>)TemplateData.ParameterData[typeof(ExchangeRate)]))
                .WithType<CreditDefaultRate>(t => t.WithKey(x => (x.Year, x.Month, x.Scenario, x.CreditRiskRating)).WithInitialData((IEnumerable<CreditDefaultRate>)TemplateData.ParameterData[typeof(CreditDefaultRate)]))
                .WithType<PartnerRating>(t => t.WithKey(x => (x.Year, x.Month, x.Scenario, x.Partner)).WithInitialData((IEnumerable<PartnerRating>)TemplateData.ParameterData[typeof(PartnerRating)]))
            ));
    }

    public static MessageHubConfiguration ConfigureParameterDataModelHub(this MessageHubConfiguration configuration)
    {
        var address = new ParameterDataAddress(configuration.Address);
        return configuration
            .WithHostedHub(address,
                config => config
                    .ConfigureParameterDataDictInit()
            );
    }

    public static MessageHubConfiguration ConfigureParameterDataImportHub(this MessageHubConfiguration configuration)
    {
        var paramDataAddress = new ParameterDataAddress(configuration.Address);
        var paramDataImportAddress = new ParameterImportAddress(configuration.Address);

        return configuration
            .WithHostedHub(paramDataImportAddress,
                config => config
                    .AddImport(data => data
                            .FromHub(paramDataAddress,dataSource => dataSource
                                .WithType<ExchangeRate>(t => t.WithKey(x => (x.Year, x.Month, x.Scenario, x.FxType, x.Currency)))
                                .WithType<CreditDefaultRate>()
                                .WithType<PartnerRating>()
                            ),
                        import => import
                    )
            );
    }

    ////Configuration 2: Use Import of TemplateParameter.CSV to Initialize the DataHub.
    //public static readonly Dictionary<Type, IEnumerable<object>> ParametersDomain =
    //    new[] { typeof(ExchangeRate), typeof(CreditDefaultRate), typeof(PartnerRating) }
    //    .ToDictionary(x => x, x => Enumerable.Empty<object>());

    //public static readonly IEnumerable<TypeDomainDescriptor> ParametersDomainExtra = new TypeDomainDescriptor[]
    //{
    //    new TypeDomainDescriptor<ExchangeRate>(){ TypeConfig = t => t.WithKey(x =>  (x.Year, x.Month, x.Scenario, x.FxType, x.Currency)) },
    //    new TypeDomainDescriptor<CreditDefaultRate>(){ TypeConfig = t => t.WithKey(x => (x.Year, x.Month, x.Scenario, x.CreditRiskRating)) },
    //    new TypeDomainDescriptor<PartnerRating>(){ TypeConfig = t => t.WithKey(x =>  (x.Year, x.Month, x.Scenario, x.Partner)) },
    //};

    //public static MessageHubConfiguration ConfigureParameterDataImportInit(this MessageHubConfiguration configuration)
    //{
    //    return configuration
    //        .AddImport(import => import)
    //        .AddData(dc => dc
    //            .WithDataSource("ParameterDataSource",
    //                ds => ds.ConfigureCategory(ParametersDomain).ConfigureCategory(ParametersDomainExtra))
    //            .WithInitialization(ParametersInit(configuration, TemplateParameter.Csv)));
    //}

    //public static Func<IMessageHub, CombinedWorkspaceState, CancellationToken, Task> ParametersInit(MessageHubConfiguration config, string csvFile)
    //{
    //    return async (hub, workspace, cancellationToken) =>
    //    {
    //        var request = new ImportRequest(csvFile);
    //        await hub.AwaitResponse(request, o => o.WithTarget(new ImportAddress(config.Address)), cancellationToken);
    //    };
    //}
}