using OpenSmc.Domain.Abstractions.Attributes;

namespace OpenSmc.Ifrs17.Domain.Import.PresentValueCalculation;

public interface IPresentValueWithInterestAccretionForCreditRisk : IPresentValue, IWithInterestAccretionForCreditRisk
{
    [NotVisible] double[] IPresentValue.Values => GetInterestAccretion();
}