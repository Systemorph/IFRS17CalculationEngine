using OpenSmc.Data;
using OpenSmc.Hierarchies;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.ServiceProvider;

namespace OpenSms.Ifrs17.CalculationScopes;

public class ImportStorage
{
    [Inject] public IWorkspace Workspace;


    public RawVariable[] GetRawVariables() => Workspace.GetData<RawVariable>().ToArray();

    public AmountType[] GetAmountTypes() => Workspace.GetData<AmountType>().ToArray();

    public ProjectionConfiguration[] GetProjectionConfigurations() => Workspace.
        GetData<ProjectionConfiguration>().ToArray();
}