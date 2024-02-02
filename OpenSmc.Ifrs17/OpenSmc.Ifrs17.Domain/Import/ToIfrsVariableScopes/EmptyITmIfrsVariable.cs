using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;

namespace OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

public interface EmptyITmIfrsVariable : ITmToIfrsVariable
{
    IEnumerable<IfrsVariable> ITmToIfrsVariable.Csms => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> ITmToIfrsVariable.Loss => Enumerable.Empty<IfrsVariable>();
    IEnumerable<IfrsVariable> ITmToIfrsVariable.AmortizationFactor => Enumerable.Empty<IfrsVariable>();
}



