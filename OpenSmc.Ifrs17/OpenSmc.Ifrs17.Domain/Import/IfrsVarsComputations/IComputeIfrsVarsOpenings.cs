using OpenSmc.Ifrs17.Domain.DataModel.TransactionalData;
using OpenSmc.Ifrs17.Domain.Import.ToIfrsVariableScopes;

namespace OpenSmc.Ifrs17.Domain.Import.IfrsVarsComputations;

public interface IComputeIfrsVarsOpenings : IActualToIfrsVariable, IDeferrableToIfrsVariable, ITmToIfrsVariable, IRevenueToIfrsVariable
{
    IEnumerable<IfrsVariable> CalculatedIfrsVariables => AdvanceActual.Concat(OverdueActual)
        .Concat(Deferrable).Concat(Csms).Concat(Loss).Concat(Revenue);
}