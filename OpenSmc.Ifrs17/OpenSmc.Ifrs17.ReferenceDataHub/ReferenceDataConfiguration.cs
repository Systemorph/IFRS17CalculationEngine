using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Activities;
using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Import;
using OpenSmc.Messaging;
using OpenSmc.Reflection;
using System.Reflection;
using OpenSmc.Ifrs17.DataTypes.Constants;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public static class DataHubConfiguration
{/*
    public record ReferenceDataAddress(object Host);
    public record DataNodeAddress(object Host);

    public static MessageHubConfiguration ConfigureIFRS17(this MessageHubConfiguration configuration, Dictionary<Type, IEnumerable<object>> types)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var dataNodeAddress = new DataNodeAddress(configuration.Address);

        return configuration
            .WithHostedHub(refDataAddress, config => config
                .WithServices(services => services.AddSingleton<IActivityService, ActivityService>()
                                                  .AddSingleton<IWorkspace, DataPlugin>())

                .AddPlugin(hub => (DataPlugin)hub.ServiceProvider.GetRequiredService<IWorkspace>())

                .WithHostedHub(new ReferenceDataAddress(config.Address), c => c.AddPlugin(h => new ImportPlugin(h, importConfig => importConfig)))


                .Set(configuration.GetListOfLambdas().Add(dc => dc))

                .WithRoutes(routes => routes
                    .RouteMessage<ImportRequest>(_ => new ImportAddress(config.Address))
                    .RouteMessage<StartDataSynchronizationRequest>(_ => new PersistenceAddress(routes.Hub.Address))
                    .RouteMessage<StopDataSynchronizationRequest>(_ => new PersistenceAddress(routes.Hub.Address))
                    .RouteMessage<DataSynchronizationState>(_ => new PersistenceAddress(routes.Hub.Address))))
            
            .WithHostedHub(dataNodeAddress, config => config
                .WithServices(services => services.AddSingleton<IActivityService, ActivityService>()
                                                  .AddSingleton<IWorkspace, DataPlugin>())

                .AddPlugin(hub => (DataPlugin)hub.ServiceProvider.GetRequiredService<IWorkspace>())

                .WithHostedHub(new DataNodeAddress(config.Address), c => c.AddPlugin(h => new ImportPlugin(h, importConfig => importConfig)))


                .Set(configuration.GetListOfLambdas().Add(dc => dc))

                .WithRoutes(routes => routes
                    .RouteMessage<ImportRequest>(_ => new ImportAddress(config.Address))
                    .RouteMessage<StartDataSynchronizationRequest>(_ => new PersistenceAddress(routes.Hub.Address))
                    .RouteMessage<StopDataSynchronizationRequest>(_ => new PersistenceAddress(routes.Hub.Address))
                    .RouteMessage<DataSynchronizationState>(_ => new PersistenceAddress(routes.Hub.Address))))
            
            .WithRoutes(routes => routes
                .RouteMessage<GetManyRequest>(_ => refDataAddress));
    }

    public static MessageHubConfiguration ConfigureIFRS17bis(this MessageHubConfiguration configuration, Dictionary<Type, IEnumerable<object>> types)
    {
        var refDataAddress = new ReferenceDataAddress(configuration.Address);
        var dataNodeAddress = new DataNodeAddress(configuration.Address);

        return configuration
                .WithServices(services => services.AddSingleton<IActivityService, ActivityService>()
                                                  .AddSingleton<IWorkspace, DataPlugin>())

                .AddPlugin(hub => (DataPlugin)hub.ServiceProvider.GetRequiredService<IWorkspace>())

                .WithHostedHub(new ReferenceDataAddress(config.Address), c => c.AddPlugin(h => new ImportPlugin(h, importConfig => importConfig)))


                .Set(configuration.GetListOfLambdas().Add(dc => dc))

                .WithRoutes(routes => routes
                    .RouteMessage<ImportRequest>(_ => new ImportAddress(config.Address))
                    .RouteMessage<StartDataSynchronizationRequest>(_ => new PersistenceAddress(routes.Hub.Address))
                    .RouteMessage<StopDataSynchronizationRequest>(_ => new PersistenceAddress(routes.Hub.Address))
                    .RouteMessage<DataSynchronizationState>(_ => new PersistenceAddress(routes.Hub.Address)));
    }
    */

    public static readonly Dictionary<Type, IEnumerable<object>> ReferenceDataDomain
        =
        new()
        {
            { typeof(AmountType), Array.Empty<AmountType>() },
            { typeof(DeferrableAmountType), new DeferrableAmountType[] {} },
            { typeof(AocType), new AocType[] {} },
            { typeof(StructureType), new StructureType[] {} },
            { typeof(CreditRiskRating), new CreditRiskRating[] {} },
            { typeof(Currency), new Currency[] {} },
            { typeof(EconomicBasis), new EconomicBasis[] {} },
            { typeof(EstimateType), new EstimateType[] {} },
            { typeof(LiabilityType), new LiabilityType[] {} },
            { typeof(LineOfBusiness), new LineOfBusiness[] {} },
            { typeof(Novelty), new  Novelty[] {} },
            { typeof(OciType), new  OciType[] {} },
            { typeof(Partner), new  Partner[] {} },
            { typeof(BsVariableType), new  BsVariableType[] {} },
            { typeof(PnlVariableType), new  PnlVariableType[] {} },
            { typeof(RiskDriver), new  RiskDriver[] {} },
            { typeof(Scenario), new  Scenario[] {} },
            { typeof(ValuationApproach), new  ValuationApproach[] {} },
            { typeof(ProjectionConfiguration), new  ProjectionConfiguration[] {} },
        };

    private static readonly IEnumerable<TypeDomainDescriptor> ReferenceDataDomainExtra =
        new TypeDomainDescriptor[]
        {
            new TypeDomainDescriptor<AocConfiguration>() { TypeConfig = t => t.WithKey(x => (x.Year, x.Month, x.AocType, x.Novelty)) },
        };

    public static MessageHubConfiguration ConfigureReferenceData(this MessageHubConfiguration configuration, Dictionary<Type, IEnumerable<object>> types)
    {
        return configuration
            .AddImport(import => import)
            .AddData(dc => dc
                .WithDataSource("ReferenceDataSource", 
                    ds => ds.ConfigureCategory(types).ConfigureCategory(ReferenceDataDomainExtra))
                .WithInitialization(InitializationAsync(configuration, TemplateDimensions.Csv)));
    }

    private static Func<IMessageHub, CancellationToken, Task> InitializationAsync(MessageHubConfiguration config, string csvFile)
    {
        return async (hub, cancellationToken) =>
        {
            var request = new ImportRequest(csvFile);
            await hub.AwaitResponse(request, o => o.WithTarget(new ImportAddress(config.Address)), cancellationToken);
        };
    }

    public static MessageHubConfiguration ConfigureReferenceDataDictInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddImport(import => import)
            .AddData(dc => dc
                .WithDataSource("ReferenceDataSource",
                    ds => ds.ConfigureCategory(TemplateData.TemplateReferenceData).ConfigureCategory(ReferenceDataDomainExtra)));
    }
}



/*  The following types and method extensions 
 *  enable types with multiple IdentityProperty
 */
public record TypeDomainDescriptor;
public record TypeDomainDescriptor<T>() : TypeDomainDescriptor where T : class
{
    public IEnumerable<T> InitialData { get; init; } = Array.Empty<T>();
    public Func<TypeSource<T>, TypeSource<T>> TypeConfig { get; init; } = typeConfig => typeConfig;
}

public static class DataSourceDomainExtensions
{
    public static DataSource ConfigureCategory(this DataSource dataSource, IEnumerable<TypeDomainDescriptor> typeDescriptors)
        => typeDescriptors.Aggregate(dataSource, (ds, t) => (DataSource)ConfigureTypeMethod.MakeGenericMethod(t.GetType().GetGenericArguments().First()).InvokeAsFunction(ds, t));

    private static readonly MethodInfo ConfigureTypeMethod = ReflectionHelper.GetStaticMethodGeneric(() => ConfigureType<object>(null, null));

    private static DataSource ConfigureType<T>(DataSource dataSource, TypeDomainDescriptor<T> typeDescriptor) where T : class
        => dataSource.WithType<T>(t => typeDescriptor.TypeConfig(t.WithInitialData(typeDescriptor.InitialData)));
}
