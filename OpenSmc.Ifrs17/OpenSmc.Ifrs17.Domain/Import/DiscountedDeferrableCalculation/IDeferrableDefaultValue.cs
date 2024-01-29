namespace OpenSmc.Ifrs17.Domain.Import.DiscountedDeferrableCalculation;

public interface IDeferrableDefaultValue : IDiscountedDeferrable
{
    double IDiscountedDeferrable.Value => default;
}