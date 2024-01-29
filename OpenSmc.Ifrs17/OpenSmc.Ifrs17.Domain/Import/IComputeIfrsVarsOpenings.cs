using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IComputeIfrsVarsOpenings : IActualToIfrsVariable, IDeferrableToIfrsVariable, ITmToIfrsVariable, IRevenueToIfrsVariable
{
    IEnumerable<IfrsVariable> CalculatedIfrsVariables => AdvanceActual.Concat(OverdueActual)
        .Concat(Deferrable).Concat(Csms).Concat(Loss).Concat(Revenue);
}