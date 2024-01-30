using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface IEmptyIPvIfrsVariable : IPvToIfrsVariable
{
    IEnumerable<IfrsVariable> IPvToIfrsVariable.PvLocked => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> IPvToIfrsVariable.PvCurrent => Enumerable.Empty<IfrsVariable>();
}