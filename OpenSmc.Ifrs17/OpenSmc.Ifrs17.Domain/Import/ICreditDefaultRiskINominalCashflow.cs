using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Arithmetics;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface ICreditDefaultRiskINominalCashflow : INominalCashflow
{
    private double[] NominalClaimsCashflow => ReferenceAocSteps.SelectMany(refAocStep =>
            GetStorage().GetClaims()
                .Select(claim => GetStorage().GetValues(Identity.Id with {AocType = refAocStep.AocType, Novelty = refAocStep.Novelty}, claim, Identity.EstimateType, Identity.AccidentYear)))
        .AggregateDoubleArray();
                            
    private string cdrBasis => Identity.AmountType == AmountTypes.CDR ? EconomicBases.C : EconomicBases.L;
    private double nonPerformanceRiskRate => GetStorage().GetNonPerformanceRiskRate(Identity.Id, cdrBasis);
                            
    private double[] PvCdrDecumulated { get {
        var ret = new double[NominalClaimsCashflow.Length];
        for (var i = NominalClaimsCashflow.Length - 1; i >= 0; i--)
            ret[i] = Math.Exp(-nonPerformanceRiskRate) * ret.ElementAtOrDefault(i + 1) + NominalClaimsCashflow[i] - NominalClaimsCashflow.ElementAtOrDefault(i + 1);
        return ret; } } 
        
    double[] INominalCashflow.Values => ArithmeticOperations.Subtract(PvCdrDecumulated, NominalClaimsCashflow);
}