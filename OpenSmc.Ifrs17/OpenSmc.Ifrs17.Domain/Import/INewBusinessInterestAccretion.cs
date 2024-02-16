using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Utils;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface INewBusinessInterestAccretion : IScope<ImportIdentity, ImportStorageOld>
{
    private int timeStep => GetStorage().GetTimeStep(Identity.ProjectionPeriod);
    private int shift => GetStorage().GetShift(Identity.ProjectionPeriod);

    double GetInterestAccretion(double[] values, string economicBasis) 
    {
        var monthlyInterestFactor = GetScope<IMonthlyRate>(Identity, o => o.WithContext(economicBasis)).Interest;
        return values.NewBusinessInterestAccretion(monthlyInterestFactor, timeStep, shift);
    }
}