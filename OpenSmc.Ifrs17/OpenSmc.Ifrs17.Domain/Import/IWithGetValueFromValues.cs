using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IWithGetValueFromValues : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    private int Shift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    private int TimeStep => 
        Identity.Id.LiabilityType == LiabilityTypes.LRC && 
        Identity.AccidentYear.HasValue && 
        (Consts.MonthInAYear * Identity.AccidentYear == (Consts.MonthInAYear * GetStorage().CurrentReportingPeriod.Year + GetStorage().GetShift(Identity.Id.ProjectionPeriod)))
            ? int.MaxValue
            : GetStorage().GetTimeStep(Identity.Id.ProjectionPeriod);

    public double GetValueFromValues(double[] Values, string overrideValuationPeriod = null)
    {
        var valuationPeriod = Enum.TryParse(overrideValuationPeriod, out ValuationPeriod ret) ? ret : GetStorage().GetValuationPeriod(Identity.Id);
        return valuationPeriod switch {
            ValuationPeriod.BeginningOfPeriod => Values.ElementAtOrDefault(Shift),
            ValuationPeriod.MidOfPeriod => Values.ElementAtOrDefault(Shift + Convert.ToInt32(Math.Round(TimeStep / 2d, MidpointRounding.AwayFromZero)) - 1),
            ValuationPeriod.Delta => Values.Skip(Shift).Take(TimeStep).Sum(),
            ValuationPeriod.EndOfPeriod  => Values.ElementAtOrDefault(Shift + TimeStep),
            ValuationPeriod.NotApplicable => default
        };
    }
}