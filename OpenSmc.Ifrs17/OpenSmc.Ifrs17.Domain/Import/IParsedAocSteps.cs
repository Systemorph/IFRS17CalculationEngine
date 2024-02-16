using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Scopes;


namespace OpenSmc.Ifrs17.Domain.Import;

public interface IParsedAocSteps : IScope<string, ImportStorageOld>
{
    IEnumerable<AocStep> Values => GetStorage().GetRawVariables(Identity).Select(x => new AocStep(x.AocType, x.Novelty)).Distinct();
}