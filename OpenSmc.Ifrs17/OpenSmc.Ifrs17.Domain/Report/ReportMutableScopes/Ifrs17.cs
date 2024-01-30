using Systemorph.Vertex.Export.Factory;
using Systemorph.Vertex.Workspace;

namespace OpenSmc.Ifrs17.Domain.Report.ReportMutableScopes;

public class Ifrs17
{
    private Systemorph.Vertex.Scopes.Proxy.IScopeFactory scopes;
    private Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory report;
    private IExportVariable export;
    private ReportStorage Storage;
    private ReportUniverse reportUniverse;

    //reset
    public void Reset(IWorkspace workspace) => Storage = new ReportStorage(workspace, report, export);

    //constructor
    public Ifrs17(IWorkspace workspace,
                   Systemorph.Vertex.Scopes.Proxy.IScopeFactory scopes,
                   Systemorph.Vertex.Pivot.Builder.Interfaces.IPivotFactory report,
                   IExportVariable export)
    {
        this.scopes = scopes;
        this.report = report;
        this.export = export;
        Storage = new ReportStorage(workspace, report, export);
        reportUniverse = scopes.ForSingleton().WithStorage(Storage).ToScope<ReportUniverse>();
    }

    public IIfrs17Report PresentValues => reportUniverse.GetScope<IIfrs17Report>(nameof(IPvReport));
    public IIfrs17Report RiskAdjustments => reportUniverse.GetScope<IIfrs17Report>(nameof(IRaReport));
    public IIfrs17Report WrittenActuals => reportUniverse.GetScope<IIfrs17Report>(nameof(IWrittenReport));
    public IIfrs17Report AccrualActuals => reportUniverse.GetScope<IIfrs17Report>(nameof(IAccrualReport));
    public IIfrs17Report DeferralActuals => reportUniverse.GetScope<IIfrs17Report>(nameof(IDeferralReport));
    public IIfrs17Report FulfillmentCashflows => reportUniverse.GetScope<IIfrs17Report>(nameof(IFcfReport));
    public IIfrs17Report ExperienceAdjustments => reportUniverse.GetScope<IIfrs17Report>(nameof(IExpAdjReport));
    public IIfrs17Report TechnicalMargins => reportUniverse.GetScope<IIfrs17Report>(nameof(ITmReport));
    public IIfrs17Report AllocatedTechnicalMargins => reportUniverse.GetScope<IIfrs17Report>(nameof(ICsmReport));
    public IIfrs17Report ActuarialLrc => reportUniverse.GetScope<IIfrs17Report>(nameof(IActLrcReport));
    public IIfrs17Report Lrc => reportUniverse.GetScope<IIfrs17Report>(nameof(ILrcReport));
    public IIfrs17Report ActuarialLic => reportUniverse.GetScope<IIfrs17Report>(nameof(IActLicReport));
    public IIfrs17Report Lic => reportUniverse.GetScope<IIfrs17Report>(nameof(ILicReport));
    public IIfrs17Report FinancialPerformance => reportUniverse.GetScope<IIfrs17Report>(nameof(IFpReport));
    public IIfrs17Report FinancialPerformanceAlternative => reportUniverse.GetScope<IIfrs17Report>(nameof(IFpAlternativeReport));
}



