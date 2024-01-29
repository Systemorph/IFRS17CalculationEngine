using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.Import.NominalCashflow;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Arithmetics;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IWithInterestAccretionForCreditRisk : IScope<(ImportIdentity Id, string AmountType, string EstimateType, int? AccidentYear), ImportStorage>
{
    private double[] NominalClaimsCashflow => GetScope<IAllClaimsCashflow>(Identity).Values;
    private double[] NominalValuesCreditRisk => ArithmeticOperations.Multiply(-1, GetScope<ICreditDefaultRiskINominalCashflow>(Identity with {Id = Identity.Id with {AocType = AocTypes.CF}}).Values);
    private double[] MonthlyInterestFactor => GetScope<IMonthlyRate>(Identity.Id).Interest;
    private string CdrBasis => Identity.AmountType == AmountTypes.CDR ? EconomicBases.C : EconomicBases.L;
    private double NonPerformanceRiskRate => GetStorage().GetNonPerformanceRiskRate(Identity.Id, CdrBasis);
    
    double[] GetInterestAccretion() 
    {
        if(!MonthlyInterestFactor.Any())
            return Enumerable.Empty<double>().ToArray();
            
        var interestOnClaimsCashflow = new double[NominalClaimsCashflow.Length];
        var interestOnClaimsCashflowCreditRisk = new double[NominalClaimsCashflow.Length];
        var effectCreditRisk = new double[NominalClaimsCashflow.Length];
        for (var i = NominalClaimsCashflow.Length - 1; i >= 0; i--) {
            interestOnClaimsCashflow[i] = 1 / MonthlyInterestFactor.GetValidElement(i/12) * (interestOnClaimsCashflow.ElementAtOrDefault(i + 1) + NominalClaimsCashflow[i] - NominalClaimsCashflow.ElementAtOrDefault(i + 1));
            interestOnClaimsCashflowCreditRisk[i] = 1 / MonthlyInterestFactor.GetValidElement(i/12) * (Math.Exp(-NonPerformanceRiskRate) * interestOnClaimsCashflowCreditRisk.ElementAtOrDefault(i + 1) + NominalClaimsCashflow[i] - NominalClaimsCashflow.ElementAtOrDefault(i + 1));
            effectCreditRisk[i] = interestOnClaimsCashflow[i] - interestOnClaimsCashflowCreditRisk[i];
        }
            
        return ArithmeticOperations.Subtract(NominalValuesCreditRisk, effectCreditRisk);
    }
}