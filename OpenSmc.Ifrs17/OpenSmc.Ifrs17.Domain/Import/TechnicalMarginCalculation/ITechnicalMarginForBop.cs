using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginForBop : ITechnicalMargin
{
    private double ValueCsm => GetStorage().GetValue(Identity, null, EstimateTypes.C, null, Identity.ProjectionPeriod);
    private double ValueLc => GetStorage().GetValue(Identity, null, EstimateTypes.L, null, Identity.ProjectionPeriod);

    double ITechnicalMargin.Value => -1d * ValueCsm + ValueLc;
}