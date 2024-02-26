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

public static class ReferenceDataHubConfiguration
{
    //Configuration 1: Use a dictionary to initialize the DataHub 
    public static MessageHubConfiguration ConfigureReferenceDataDictInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddData(dc => dc
                .WithDataSource("ReferenceDataSource",
                    ds => ds.ConfigureCategory(TemplateData.TemplateReferenceData)
                        .WithType<AocConfiguration>(t => t.WithKey(x => (x.Year, x.Month, x.AocType, x.Novelty)).WithInitialData(TemplateData.AocConfiguration[typeof(AocConfiguration)]))
                ));
    }

    //Configuration 2: Use Import of Dimension.CSV to Initialize the DataHub
    public static readonly Dictionary<Type, IEnumerable<object>> ReferenceDataDomain
    =
    new[] { typeof(AmountType), typeof(DeferrableAmountType), typeof(AocType), typeof(StructureType),
            typeof(CreditRiskRating), typeof(Currency), typeof(EconomicBasis), typeof(EstimateType),
            typeof(LiabilityType), typeof(LineOfBusiness), typeof(Novelty), typeof(OciType),
            typeof(Partner), typeof(BsVariableType), typeof(PnlVariableType), typeof(RiskDriver),
            typeof(Scenario), typeof(ValuationApproach), typeof(ProjectionConfiguration) }
    .ToDictionary(x => x, x => Enumerable.Empty<object>());
    
    public static MessageHubConfiguration ConfigureReferenceDataImportInit(this MessageHubConfiguration configuration)
    {
        return configuration
            .AddImport(import => import)
            .AddData(dc => dc
                .WithDataSource("ReferenceDataSource", 
                    ds => ds.ConfigureCategory(ReferenceDataDomain).ConfigureCategory(ReferenceDataDomainExtra))
                .WithInitialization(RefDataInit(configuration, TemplateDimensions.Csv)));
    }

    public static Func<IMessageHub, CancellationToken, Task> RefDataInit(MessageHubConfiguration config, string csvFile)
    {
        return async (hub, cancellationToken) =>
        {
            var request = new ImportRequest(csvFile);
            await hub.AwaitResponse(request, o => o.WithTarget(new ImportAddress(config.Address)), cancellationToken);
        };
    }

    public static readonly IEnumerable<TypeDomainDescriptor> ReferenceDataDomainExtra = new TypeDomainDescriptor[]
    {
        new TypeDomainDescriptor<AocConfiguration>() 
        { TypeConfig = t => t.WithKey(x => (x.Year, x.Month, x.AocType, x.Novelty)) },
    };
}

/*  The following types and method extensions 
 *  enable types with multiple IdentityProperty */
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
