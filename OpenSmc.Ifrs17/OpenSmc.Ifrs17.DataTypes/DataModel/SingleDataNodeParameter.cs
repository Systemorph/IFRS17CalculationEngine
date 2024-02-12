using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.Domain.Constants;
using OpenSmc.Ifrs17.Domain.Constants.Enumerates;
using OpenSmc.Ifrs17.Domain.DataModel.KeyedDimensions;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record SingleDataNodeParameter : DataNodeParameter
{
    [DefaultValue(Consts.DefaultPremiumExperienceAdjustmentFactor)]
    [Range(0, 1, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [Display(Order = 20)]
    public double PremiumAllocation { get; init; } = Consts.DefaultPremiumExperienceAdjustmentFactor;

    [Dimension(typeof(CashFlowPeriodicity))]
    [Display(Order = 30)]
    public CashFlowPeriodicity CashFlowPeriodicity { get; init; }

    [Dimension(typeof(InterpolationMethod))]
    [Display(Order = 40)]
    public InterpolationMethod InterpolationMethod { get; init; }

    [Dimension(typeof(EconomicBasis))]
    [Display(Order = 50)]
    public string EconomicBasisDriver { get; init; }

    //[Conversion(typeof(PrimitiveArrayConverter))]
    [Display(Order = 60)]
    public double[] ReleasePattern { get; init; }
}