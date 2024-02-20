namespace OpenSmc.Ifrs17.CalculationScopes.NominalCashflow;

public interface IAllClaimsCashflow : INominalCashflow
{
    double[] INominalCashflow.Values => ReferenceAocSteps.SelectMany(refAocStep =>
            GetStorage().GetClaims()
                .Select(claim => GetStorage().GetValues(Identity.Id with { AocType = refAocStep.AocType, Novelty = refAocStep.Novelty }, claim, Identity.EstimateType, Identity.AccidentYear)))
        .AggregateDoubleArray();
}