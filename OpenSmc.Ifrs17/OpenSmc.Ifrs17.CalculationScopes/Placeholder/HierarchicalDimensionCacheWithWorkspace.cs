using OpenSmc.Data;
using OpenSmc.DataCubes;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Hierarchies;
using OpenSmc.Reflection;

namespace OpenSmc.Ifrs17.CalculationScopes.Placeholder;

public class HierarchicalDimensionCacheWithWorkspace : IHierarchicalDimensionCache
{
    private readonly IWorkspace _workspace;
    private readonly Dictionary<Type, IHierarchy> _cachedDimensions = new();

    public HierarchicalDimensionCacheWithWorkspace(IWorkspace workspace)
    {
        _workspace = workspace;
    }

    public IHierarchyNode<T> Get<T>(string systemName)
        where T : class, IHierarchicalDimension
    {
        return Get<T>()?.GetHierarchyNode(systemName);
    }

    public IHierarchy<T> Get<T>()
        where T : class, IHierarchicalDimension
    {
        if (!_cachedDimensions.TryGetValue(typeof(T), out var inner))
            return null;
        return (IHierarchy<T>)inner;
    }

    public async Task InitializeAsync(params DimensionDescriptor[] dimensionDescriptors)
    {
        foreach (var type in dimensionDescriptors.Where(d => d.Type != null).Select(d => d.Type))
        {
            if (typeof(IHierarchicalDimension).IsAssignableFrom(type))
                await InitializeAsyncMethod.MakeGenericMethod(type).InvokeAsActionAsync(this);
        }
    }

    private static readonly IGenericMethodCache InitializeAsyncMethod =
#pragma warning disable 4014
        GenericCaches.GetMethodCache<HierarchicalDimensionCacheWithWorkspace>(x => x.InitializeAsync<IHierarchicalDimension>());
#pragma warning restore 4014

    public async Task InitializeAsync<T>()
        where T : class, IHierarchicalDimension
    {
        if (_workspace != null && !_cachedDimensions.TryGetValue(typeof(T), out _))
        {
            var hierarchy = new HierarchyWithWorkspace<T>(_workspace);
            await hierarchy.InitializeAsync();
            _cachedDimensions[typeof(T)] = hierarchy;
        }
    }

    public void Initialize<T>(IDictionary<string, T> outerElementsBySystemName)
        where T : class, IHierarchicalDimension
    {
        if (outerElementsBySystemName != null && !_cachedDimensions.TryGetValue(typeof(T), out _))
        {
            var hierarchy = new HierarchyWithWorkspace<T>(outerElementsBySystemName);
            hierarchy.Initialize();
            _cachedDimensions[typeof(T)] = hierarchy;
        }
    }
}