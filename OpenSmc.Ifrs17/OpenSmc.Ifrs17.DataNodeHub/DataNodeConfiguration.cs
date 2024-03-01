using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.ReferenceDataHub;
using OpenSmc.Ifrs17.Utils;
using OpenSmc.Import;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.DataNodeHub;

public static class DataNodeHubConfiguration
{
    public static MessageHubConfiguration ConfigureDataNodeDataDictInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc
                .FromConfigurableDataSource("DataNodeDataSource",
                    ds => ds.ConfigureCategory(TemplateData.DataNodeData)
                ));
    }

    //public static readonly Dictionary<Type, IEnumerable<object>> DataNodeDomain
    //= 
    //new[] { typeof(InsurancePortfolio), typeof(ReinsurancePortfolio), 
    //        typeof(GroupOfInsuranceContract), typeof(GroupOfReinsuranceContract) }
    //.ToDictionary(x => x, x => Enumerable.Empty<object>());

    //public static MessageHubConfiguration ConfigureRefDataAndDataNodes(this MessageHubConfiguration configuration)
    //{
    //    // TODO: this is WIP (2024.02.26, AM)
    //    var refDataAddress = new ReferenceDataAddress(configuration.Address);
    //    var dataNodeAddress = new DataNodeAddress(configuration.Address);

    //    return configuration

    //        .WithHostedHub(refDataAddress, config => config
    //            .AddImport(import => import)
    //            .AddData(dc => dc
    //            .WithDataSource("ReferenceDataSource",
    //                ds => ds.ConfigureCategory(ReferenceDataHubConfiguration.ReferenceDataDomain)
    //                        .ConfigureCategory(ReferenceDataHubConfiguration.ReferenceDataDomainExtra))
    //            .WithInitialization(ReferenceDataHubConfiguration.RefDataInit(configuration, TemplateDimensions.Csv))))

    //        .WithHostedHub(dataNodeAddress, config => config
    //            .AddImport(import => import)
    //            .AddData(dc => dc
    //            .WithDataSource("DataNodeDataSource",
    //                ds => ds.ConfigureCategory(DataNodeDomain).ConfigureCategory(DataNodeDomainExtra))
    //            .WithInitialization(DataNodeInit(configuration, TemplateDimensions.Csv, refDataAddress))))

    //        .WithRoutes(routes => routes
    //            .RouteMessage<GetManyRequest>(_ => refDataAddress));
    //}

    //public static Func<IMessageHub, CombinedWorkspaceState, CancellationToken, Task> DataNodeInit(MessageHubConfiguration config, string csvFile, ReferenceDataAddress refDataAddress)
    //{
    //    var refDataRequired = new[] { typeof(ValuationApproach), typeof(Currency), typeof(LineOfBusiness), typeof(OciType),
    //                                  typeof(LiabilityType), typeof(Profitability), typeof(YieldCurve), typeof(Partner) };

    //    return async (hub, workspace, cancellationToken) =>
    //    {
    //        var refDataRequest = new GetManyRequest(refDataRequired);
    //        await hub.AwaitResponse(refDataRequest, o => o.WithTarget(refDataAddress), cancellationToken);
    //        var importRequest = new ImportRequest(csvFile);
    //        await hub.AwaitResponse(importRequest, o => o.WithTarget(new ImportAddress(config.Address)), cancellationToken);
    //    };
    //}

    //public static readonly IEnumerable<TypeDomainDescriptor> DataNodeDomainExtra = new TypeDomainDescriptor[]
    //{
    //    new TypeDomainDescriptor<AocConfiguration>()
    //    { TypeConfig = t => t.WithKey(x => (x.Year, x.Month, x.AocType, x.Novelty)) },
    //};
}
