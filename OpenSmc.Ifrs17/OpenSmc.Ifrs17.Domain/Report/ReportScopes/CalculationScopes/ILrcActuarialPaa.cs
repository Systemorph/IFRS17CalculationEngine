using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.DataModel;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.DataCubes;
using Systemorph.Vertex.DataCubes.Api;

namespace OpenSmc.Ifrs17.Domain.Report.ReportScopes.CalculationScopes;

public interface ILrcActuarialPaa : ILrcActuarial
{
    IDataCube<ReportVariable> ILrcActuarial.LrcActuarial =>
        -1d * GetScope<IRevenues>(Identity).Revenues + -1d * GetScope<IDeferrals>(Identity).Deferrals + Loreco
        + GetScope<IBestEstimate>(Identity).BestEstimate
            .Where(x => GetStorage()
                .GetHierarchy<AmountType>()
                .Ancestors(x.AmountType, true).
                Any(y => y.SystemName == AmountTypes.PR) || GetStorage().GetHierarchy<AmountType>()
                .Ancestors(x.AmountType, true).Any(z => z.SystemName == AmountTypes.DE))
            .ToDataCube();
}