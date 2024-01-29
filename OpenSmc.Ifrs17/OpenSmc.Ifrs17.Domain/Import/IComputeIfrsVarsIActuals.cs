using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IComputeIfrsVarsIActuals : IActualToIfrsVariable, IDeferrableToIfrsVariable, IEaForPremiumToIfrsVariable, ITmToIfrsVariable
{
    IEnumerable<IfrsVariable> CalculatedIfrsVariables => Actual.Concat(AdvanceActual).Concat(OverdueActual)
        .Concat(ActEAForPremium).Concat(Deferrable).Concat(Csms).Concat(Loss);
}