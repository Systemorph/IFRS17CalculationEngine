using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Arithmetics;

namespace OpenSmc.Ifrs17.Domain.Import;

public interface IDiscountedCreditRiskCashflow : IDiscountedCashflow{
    private string CdrBasis => Identity.AmountType == AmountTypes.CDR ? EconomicBases.C : EconomicBases.L;
    private double NonPerformanceRiskRate => GetStorage().GetNonPerformanceRiskRate(Identity.Id, CdrBasis);
        
    double[] IDiscountedCashflow.Values => ArithmeticOperations.Multiply(-1d, NominalValues.ComputeDiscountAndCumulateWithCreditDefaultRisk(MonthlyDiscounting, NonPerformanceRiskRate)); // we need to flip the sign to create a reserve view
}