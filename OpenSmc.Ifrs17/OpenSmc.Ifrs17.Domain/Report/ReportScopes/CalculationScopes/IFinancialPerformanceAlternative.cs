using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IFinancialPerformanceAlternative : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> InsuranceRevenue => GetScope<IInsuranceRevenue>(Identity);
    private IDataCube<ReportVariable> InsuranceServiceExpense => GetScope<IInsuranceServiceExpense>(Identity);
    private IDataCube<ReportVariable> InsuranceFinanceIncomeExpenseOci => GetScope<IInsuranceFinanceIncomeExpenseOci>(Identity);

    IDataCube<ReportVariable> FinancialPerformanceAlternative => InsuranceRevenue + InsuranceServiceExpense + InsuranceFinanceIncomeExpenseOci;
}