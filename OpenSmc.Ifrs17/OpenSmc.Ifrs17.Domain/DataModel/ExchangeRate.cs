using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;
using Systemorph.Vertex.Api.Attributes;
using Systemorph.Vertex.Arithmetics.Api;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record ExchangeRate : KeyedRecord, IWithYearMonthAndScenario
{
    [Required]
    [IdentityProperty]
    [Dimension(typeof(Currency))]
    public string Currency { get; init; }

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

    [IdentityProperty][Required] public FxType FxType { get; init; }

    public double FxToGroupCurrency { get; init; }

    [IdentityProperty]
    [Dimension(typeof(Scenario))]
    public string Scenario { get; init; }
}