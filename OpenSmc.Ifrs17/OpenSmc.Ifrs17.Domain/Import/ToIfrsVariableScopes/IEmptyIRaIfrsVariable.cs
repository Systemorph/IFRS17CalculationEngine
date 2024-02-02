using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface IEmptyIRaIfrsVariable : IRaToIfrsVariable
{
    IEnumerable<IfrsVariable> IRaToIfrsVariable.RaCurrent => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> IRaToIfrsVariable.RaLocked => Enumerable.Empty<IfrsVariable>();
}