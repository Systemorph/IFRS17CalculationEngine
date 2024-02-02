using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;
using OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

namespace OpenSmc.Ifrs17.Domain.Import.IfrsVarsComputations;

public interface IComputeIfrsVarsIActuals : IActualToIfrsVariable, IDeferrableToIfrsVariable, IEaForPremiumToIfrsVariable, ITmToIfrsVariable
{
    IEnumerable<IfrsVariable> CalculatedIfrsVariables => Actual.Concat(AdvanceActual).Concat(OverdueActual)
        .Concat(ActEAForPremium).Concat(Deferrable).Concat(Csms).Concat(Loss);
}