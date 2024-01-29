using OpenSmc.Ifrs17.Domain.Constants;

namespace OpenSmc.Ifrs17.Domain.Import.TechnicalMarginCalculation;

public interface ITechnicalMarginAfterFirstYear : ITechnicalMargin
{
    double ITechnicalMargin.Value => GetScope<ITechnicalMargin>(Identity with { AocType = AocTypes.EOP, Novelty = Novelties.C, ProjectionPeriod = Identity.ProjectionPeriod - 1 }).Value;
}