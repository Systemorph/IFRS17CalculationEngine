using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IEmptyIRaIfrsVariable: IRaToIfrsVariable{
    IEnumerable<IfrsVariable> IRaToIfrsVariable.RaCurrent => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> IRaToIfrsVariable.RaLocked => Enumerable.Empty<IfrsVariable>();
}