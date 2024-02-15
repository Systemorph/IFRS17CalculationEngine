using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Scopes;


namespace OpenSms.Ifrs17.CalculationScopes.AocSteps;

public interface IParsedAocSteps : IScope<string>
{
    IEnumerable<AocStep> Values => GetStorage().GetRawVariables(Identity).Select(x => new AocStep(x.AocType, x.Novelty)).Distinct();
}