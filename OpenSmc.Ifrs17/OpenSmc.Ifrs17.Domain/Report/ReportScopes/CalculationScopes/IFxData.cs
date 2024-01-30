using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Ifrs17.Domain.Utils;
using Systemorph.Vertex.Arithmetics;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;
using Systemorph.Vertex.Scopes;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface IFxData : IScope<(ReportIdentity ReportIdentity, CurrencyType CurrencyType, string EstimateType), ReportStorage>, IDataCube<ReportVariable>
{
    static ApplicabilityBuilder ScopeApplicabilityBuilder(ApplicabilityBuilder builder)
    {
        return builder.ForScope<IFxData>(s => s.WithApplicability<IFxDataWrittenActual>(x => x.GetStorage().EstimateTypesWithoutAoc.Contains(x.Identity.EstimateType)));
    }

    protected IDataCube<ReportVariable> Data => GetScope<IDataScope>((Identity.ReportIdentity, Identity.EstimateType)).Data
        .SelectToDataCube(x => ArithmeticOperations.Multiply(GetScope<IFx>((Identity.ReportIdentity.ContractualCurrency,
                Identity.ReportIdentity.FunctionalCurrency,
                GetStorage().GetFxPeriod(GetStorage().Args.Period, Identity.ReportIdentity.Projection, x.VariableType, x.Novelty),
                (Identity.ReportIdentity.Year, Identity.ReportIdentity.Month),
                Identity.CurrencyType)).Fx, x) with
        {
            Currency = Identity.CurrencyType switch
            {
                CurrencyType.Contractual => x.ContractualCurrency,
                CurrencyType.Functional => x.FunctionalCurrency,
                _ => Consts.GroupCurrency
            }
        });

    private IDataCube<ReportVariable> Eops => Data.Filter(("VariableType", AocTypes.EOP));
    private IDataCube<ReportVariable> NotEops => Data.Filter(("VariableType", "!EOP")); // TODO negation must be hardcoded (also to avoid string concatenation)

    private IDataCube<ReportVariable> Fx => (Eops - NotEops)
        .AggregateOver(nameof(Novelty), nameof(VariableType))
        .SelectToDataCube(x => Math.Abs(x.Value) >= Consts.Precision, x => x with { Novelty = Novelties.C, VariableType = AocTypes.FX });

    IDataCube<ReportVariable> FxData => Data + Fx;
}