using OpenSmc.Ifrs17.DataTypes.DataModel;
using OpenSmc.Ifrs17.DataTypes.DataModel.TransactionalData;
using OpenSmc.Scopes;

namespace OpenSms.Ifrs17.CalculationScopes;

public interface IRawVariable : IScope<(ImportIdentity Id, string AmounType, string EstimateType, int? AccidentYear), ImportStorage>
{
    public IEnumerable<RawVariable> GetRawVariables() => GetStorage().GetRawVariables(); // Should the request come here?
    public IEnumerable<double> GetValues() => GetRawVariables().GetValues(Identity.Id, _ => true);
}