using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface IEmptyINominalToIfrsVariable : INominalToIfrsVariable
{
    IEnumerable<IfrsVariable> INominalToIfrsVariable.CumulatedNominal => Enumerable.Empty<IfrsVariable>();
}