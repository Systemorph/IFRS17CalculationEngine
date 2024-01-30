using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IFinancialPerformance : IScope<(ReportIdentity Id, CurrencyType CurrencyType), ReportStorage>, IDataCube<ReportVariable>
{
    private IDataCube<ReportVariable> FcfChangeInEstimate => GetScope<IFcfChangeInEstimate>(Identity);
    private IDataCube<ReportVariable> CsmChangeInEstimate => GetScope<ICsmChangeInEstimate>(Identity);
    private IDataCube<ReportVariable> LcChangeInEstimate => GetScope<ILcChangeInEstimate>(Identity);
    private IDataCube<ReportVariable> LorecoChangeInEstimate => GetScope<ILorecoChangeInEstimate>(Identity);
    private IDataCube<ReportVariable> IncurredActuals => GetScope<IIncurredActuals>(Identity);
    private IDataCube<ReportVariable> IncurredDeferrals => GetScope<IIncurredDeferrals>(Identity);
    private IDataCube<ReportVariable> ExperienceAdjustmentOnPremium => GetScope<IExperienceAdjustmentOnPremium>(Identity);
    //private IDataCube<ReportVariable> ExperienceAdjustmentOnAcquistionExpenses => GetScope<ExperienceAdjustmentOnAcquistionExpenses>(Identity);

    IDataCube<ReportVariable> FinancialPerformance => FcfChangeInEstimate + CsmChangeInEstimate + LcChangeInEstimate + LorecoChangeInEstimate + IncurredActuals + IncurredDeferrals + ExperienceAdjustmentOnPremium;
}