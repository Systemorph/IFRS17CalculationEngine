using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;

namespace OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;

public interface IEopDeferrable : INominalDeferrable
{
    private IEnumerable<AocStep> PreviousAocSteps => GetScope<IPreviousAocSteps>((Identity.Id, StructureType.AocTechnicalMargin)).Values;
    double INominalDeferrable.Value => PreviousAocSteps.Sum(aocStep => GetScope<INominalDeferrable>((Identity.Id with { AocType = aocStep.AocType, Novelty = aocStep.Novelty }, Identity.MonthlyShift)).Value);
}