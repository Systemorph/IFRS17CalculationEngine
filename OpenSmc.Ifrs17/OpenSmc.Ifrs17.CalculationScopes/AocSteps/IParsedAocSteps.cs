using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.CalculationScopes.AocSteps;

public interface IParsedAocSteps : IScope<string, ImportStorage>
{
    IEnumerable<AocStep> Values => GetStorage().GetRawVariables(Identity).Select(x => new AocStep(x.AocType, x.Novelty)).Distinct();
}