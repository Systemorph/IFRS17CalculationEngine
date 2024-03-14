using OpenSmc.Collections;
using OpenSmc.Data;
using OpenSmc.Domain.Abstractions;
using OpenSmc.Hierarchies;

namespace OpenSmc.Ifrs17.CalculationScopes.Placeholder;

public class HierarchyWithWorkspace<T> : IHierarchy<T>
    where T : class, IHierarchicalDimension
{
    private IDictionary<string, T> elementsBySystemName;
    private readonly IDictionary<string, IDictionary<int, string>> elementsBySystemNameAndLevels;
    private readonly IDictionary<int, IList<string>> dimensionsByLevel;
    private readonly IWorkspace _workspace;

    public HierarchyWithWorkspace(IWorkspace workspace)
    {
        _workspace = workspace;
        dimensionsByLevel = new Dictionary<int, IList<string>>();
        elementsBySystemNameAndLevels = new Dictionary<string, IDictionary<int, string>>();
    }

    public HierarchyWithWorkspace(IDictionary<string, T> outerElementsBySystemName)
    {
        elementsBySystemName = outerElementsBySystemName;
        dimensionsByLevel = new Dictionary<int, IList<string>>();
        elementsBySystemNameAndLevels = new Dictionary<string, IDictionary<int, string>>();
    }

    public async Task InitializeAsync()
    {
        elementsBySystemName = await _workspace.GetData<T>().ToAsyncEnumerable().ToDictionaryAsync(x => x.SystemName);
        AddChildren(0, GetPairs());
    }

    public void Initialize()
    {
        if (elementsBySystemName != null) AddChildren(0, GetPairs());
    }

    private IEnumerable<ChildParent> GetPairs()
    {
        return from dim in elementsBySystemName.Values
               join parent in elementsBySystemName.Values on dim.Parent equals parent.SystemName into joined
               from parent in joined.DefaultIfEmpty()
               select new ChildParent { Child = dim, Parent = parent };
    }

    public T Get(string systemName)
    {
        if (systemName == null || !elementsBySystemName.TryGetValue(systemName, out var ret))
            return null;
        return ret;
    }

    public IHierarchyNode<T> GetHierarchyNode(string systemName)
    {
        if (systemName == null || !elementsBySystemName.ContainsKey(systemName))
            return null;
        return new HierarchyNode<T>(systemName, this);
    }

    private readonly Dictionary<string, T[]> children = new();

    public T[] Children(string systemName)
    {
        systemName ??= "";

        return children.GetOrAdd(systemName, _ => elementsBySystemName.Values.Where(x => x.Parent == systemName).ToArray());
    }

    private readonly Dictionary<string, T[]> descendants = new();
    private readonly Dictionary<string, T[]> descendantWithSelf = new();

    public T[] Descendants(string systemName, bool includeSelf = false)
    {
        systemName ??= "";

        if (includeSelf) return descendantWithSelf.GetOrAdd(systemName, _ => elementsBySystemNameAndLevels.Where(x => x.Value.Values.Contains(systemName))
            .Select(x => elementsBySystemName[x.Key])
            .ToArray());

        return descendants.GetOrAdd(systemName, _ => elementsBySystemNameAndLevels.Where(x => x.Key != systemName && x.Value.Values.Contains(systemName))
            .Select(x => elementsBySystemName[x.Key])
            .ToArray());
    }

    private readonly Dictionary<string, T[]> ancestors = new();
    private readonly Dictionary<string, T[]> ancestorsSelf = new();

    public T[] Ancestors(string systemName, bool includeSelf = false)
    {
        systemName ??= "";

        if (!elementsBySystemNameAndLevels.TryGetValue(systemName, out var levels)) return default;

        if (includeSelf) return ancestorsSelf.GetOrAdd(systemName, _ => levels.Select(x => elementsBySystemName[x.Value]).ToArray());

        return ancestors.GetOrAdd(systemName, _ => levels.Where(x => x.Value != systemName).Select(x => elementsBySystemName[x.Value]).ToArray());
    }

    private readonly Dictionary<string, T[]> siblings = new();
    private readonly Dictionary<string, T[]> siblingsSelf = new();

    public T[] Siblings(string systemName, bool includeSelf = false)
    {
        systemName ??= "";

        if (includeSelf) return siblingsSelf.GetOrAdd(systemName, _ => elementsBySystemName.Values.Where(x => x.Parent == Get(systemName).Parent).ToArray());

        return siblings.GetOrAdd(systemName, _ => elementsBySystemName.Values.Where(x => x.Parent == Get(systemName).Parent && x.SystemName != systemName).ToArray());
    }

    public int Level(string systemName)
    {
        if (!elementsBySystemNameAndLevels.TryGetValue(systemName, out var levels)) return 0;
        return levels.Keys.Max();
    }

    public T AncestorAtLevel(string systemName, int level)
    {
        if (!elementsBySystemNameAndLevels.TryGetValue(systemName, out var levels)) return null;
        if (!levels.TryGetValue(level, out var dimName)) return null;
        elementsBySystemName.TryGetValue(dimName, out var dim);
        return dim;
    }

    private readonly Dictionary<(string, int), T[]> descendantsAtLevel = new();

    public T[] DescendantsAtLevel(string systemName, int level)
    {
        systemName ??= "";

        return descendantsAtLevel.GetOrAdd((systemName, level),
            _ => elementsBySystemNameAndLevels.Where(x => x.Value.Values.Contains(systemName) && x.Value.Keys.Max() == level)
                .Select(x => elementsBySystemName[x.Key])
                .ToArray());
    }

    private class ChildParent
    {
        public T Child { get; init; }
        public T Parent { get; init; }
    }

    private void AddChildren(int level, IEnumerable<ChildParent> pairs)
    {
        var dimensionsFormPreviousLevel = level == 0
            ? new List<string>()
            : dimensionsByLevel[level - 1];

        var dimensionsByThisLevel = pairs.GroupBy(x => level == 0 ? x.Parent == null : dimensionsFormPreviousLevel.Contains(x.Parent.SystemName))
            .ToDictionary(x => x.Key, y => y);

        if (dimensionsByThisLevel.TryGetValue(true, out var dimensionsOnThisLevel))
        {
            foreach (var dim in dimensionsOnThisLevel)
            {
                elementsBySystemNameAndLevels[dim.Child.SystemName] = new Dictionary<int, string>
                {
                    { level, dim.Child.SystemName }
                };

                if (dim.Parent != null)
                {
                    foreach (var element in elementsBySystemNameAndLevels[dim.Parent.SystemName])
                        elementsBySystemNameAndLevels[dim.Child.SystemName].Add(element.Key, element.Value);
                }
            }

            dimensionsByLevel[level] = dimensionsOnThisLevel.Select(x => x.Child.SystemName).ToList();
        }

        if (dimensionsByThisLevel.TryGetValue(false, out var otherDimensions))
            AddChildren(level + 1, otherDimensions);
    }
}