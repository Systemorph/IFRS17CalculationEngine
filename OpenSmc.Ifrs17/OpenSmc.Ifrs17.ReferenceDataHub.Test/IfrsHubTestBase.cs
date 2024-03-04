using OpenSmc.Data;
using OpenSmc.Messaging;
using OpenSmc.Reflection;
using System.Reflection;
using OpenSmc.Hub.Fixture;
using Xunit.Abstractions;

namespace OpenSmc.Ifrs17.Hub.Test
{
    public class IfrsHubTestBase(ITestOutputHelper output) : HubTestBase(output)
    {
        public static async Task<Dictionary<Type, int>> GetActualCountsPerType(IMessageHub client, IEnumerable<Type> types, object address)
        {
            var actualCountsPerType = new Dictionary<Type, int>();
            foreach (var domainType in types)
            {
                var requestType = typeof(GetManyRequest<>).MakeGenericType(domainType);
                var request = Activator.CreateInstance(requestType);
                var responseType = typeof(GetManyResponse<>).MakeGenericType(domainType);
                Func<PostOptions, PostOptions> options = o => o.WithTarget(address);
                object response = (((IMessageDelivery)await AwaitResponseMethod.MakeGenericMethod(responseType)
                    .InvokeAsFunctionAsync(client, request, options)).Message);
                var total = ((GetManyResponseBase)response).Total;
                actualCountsPerType[domainType] = total;
            }

            return actualCountsPerType;
        }

        private static readonly MethodInfo AwaitResponseMethod = ReflectionHelper.GetMethodGeneric<IMessageHub>(x => x.AwaitResponse<object>(null, null));

    }
}
