using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IInterestAccretionFactor : IScope<ImportIdentity, ImportStorage>{
    private int TimeStep => GetStorage().GetTimeStep(Identity.ProjectionPeriod);
    private int Shift => GetStorage().GetShift(Identity.ProjectionPeriod);
    
    double GetInterestAccretionFactor(string economicBasis) 
    {
        double[] monthlyInterestFactor = GetScope<IMonthlyRate>(Identity, o => o.WithContext(economicBasis)).Interest;
        return Enumerable.Range(Shift,TimeStep).Select(i => monthlyInterestFactor.GetValidElement(i/12)).Aggregate(1d, (x, y) => x * y ) - 1d;
    }
}