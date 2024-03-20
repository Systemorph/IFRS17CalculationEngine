using OpenSmc.Data;
using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.FinancialDataDimensions;
using OpenSmc.Scopes;

namespace OpenSmc.Ifrs17.CalculationScopes.QueryScopes
{
    internal interface IAmountType : IScope<(ImportIdentity Identity, string name), ImportStorage>
    {
        public AmountType[] GetAmountTypes() => GetStorage().Workspace
            .GetData<AmountType>().ToArray();
    }
}
