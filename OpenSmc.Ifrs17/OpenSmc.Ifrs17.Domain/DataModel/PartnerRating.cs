using System.ComponentModel.DataAnnotations;
using OpenSmc.Arithmetics;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using OpenSmc.Domain.Abstractions.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record PartnerRating : KeyedRecord, IWithYearMonthAndScenario
{
    [Required]
    [IdentityProperty]
    [Dimension(typeof(Partner))]
    public string Partner { get; init; }

    [Required]
    [Dimension(typeof(CreditRiskRating))]
    public string CreditRiskRating { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Year))]
    [Range(1900, 2100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Year { get; init; }

    [Required]
    [IdentityProperty]
    [NoArithmetics(ArithmeticOperation.Scale)]
    [Dimension(typeof(int), nameof(Month))]
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Month { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }
}