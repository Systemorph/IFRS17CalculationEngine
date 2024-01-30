using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface IEmptyINominalToIfrsVariable : INominalToIfrsVariable
{
    IEnumerable<IfrsVariable> INominalToIfrsVariable.CumulatedNominal => Enumerable.Empty<IfrsVariable>();
}