using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ParameterDataHub;

public static class ParameterHubConfiguration
{
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
                                .WithType<ExchangeRate>()
                                .WithType<CreditDefaultRate>()
                                .WithType<PartnerRating>()
                            ),
                        import => import
                    )
            );
    }
}