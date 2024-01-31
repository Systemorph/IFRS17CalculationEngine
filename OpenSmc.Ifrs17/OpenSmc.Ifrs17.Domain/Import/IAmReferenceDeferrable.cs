using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;
using OpenSmc.Ifrs17.Domain.Import.NominalDeferrableCalculation;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IAmReferenceDeferrable: IScope<(ImportIdentity Id, int MonthlyShift), ImportStorage>{
    private int ProjectionShift => GetStorage().GetShift(Identity.Id.ProjectionPeriod);
    private IEnumerable<AocStep> PreviousAocSteps => GetScope<IPreviousAocSteps>((Identity.Id, StructureType.AocTechnicalMargin)).Values.Where(aocStep => aocStep.Novelty != Novelties.C);
    double ReferenceCashflow => PreviousAocSteps
        .GroupBy(x => x.Novelty, (_, aocs) => aocs.Last())
        .Sum(aoc => GetScope<INominalCashflow>((Identity.Id with {AocType = aoc.AocType, Novelty = aoc.Novelty}, AmountTypes.DAE, EstimateTypes.BE, (int?)null)).Values
            .Skip(ProjectionShift + Identity.MonthlyShift).FirstOrDefault());
    //if no previous RawVariable, use IfrsVariable
    double Value => Math.Abs(ReferenceCashflow) >= Consts.Precision ? ReferenceCashflow : GetStorage().GetNovelties(AocTypes.BOP, StructureType.AocPresentValue).Sum(n => GetScope<INominalDeferrable>((Identity.Id with {AocType = AocTypes.BOP, Novelty = n}, Identity.MonthlyShift)).Value);
}