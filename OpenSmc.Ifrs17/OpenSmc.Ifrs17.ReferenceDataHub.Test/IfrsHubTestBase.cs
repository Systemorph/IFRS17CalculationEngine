using System.Reactive.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenSmc.Data;
using OpenSmc.Messaging;
using OpenSmc.Reflection;
using OpenSmc.Hub.Fixture;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Hub.Test
{
    public class IfrsHubTestBase(ITestOutputHelper output) : HubTestBase(output)
    {
        public static async Task<Dictionary<Type, int>> GetActualCountsPerType(IMessageHub client, IEnumerable<Type> types, object address)  // TODO V10: address is redundant here (2024/03/21, Dmitry Kalabin)
        {
            var workspace = client.GetWorkspace();
            var actualCountsPerType = new Dictionary<Type, int>();
            foreach (var domainType in types)
            {
                var total = (int) await GetCountMethod.MakeGenericMethod(domainType).InvokeAsFunctionAsync(workspace);
                actualCountsPerType[domainType] = total;
            }

            return actualCountsPerType;
        }

        private static readonly MethodInfo GetCountMethod = ReflectionHelper.GetStaticMethodGeneric(() => GetCount<object>(null));

        private static async Task<int> GetCount<T>(IWorkspace workspace) => (await GetData<T>(workspace)).Count;
        private static IObservable<IReadOnlyCollection<T>> GetData<T>(IWorkspace workspace) => workspace.GetObservable<T>().FirstAsync();
    }

    public static class WorkspaceHubExtensions
    {
        public static IWorkspace GetWorkspace(this IMessageHub hub) => hub.ServiceProvider.GetRequiredService<IWorkspace>();
    }
}
