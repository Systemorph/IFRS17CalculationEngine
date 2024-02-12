using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenSmc.Domain.Abstractions.Attributes;


namespace OpenSmc.Ifrs17.Domain.DataModel;

public record GroupOfInsuranceContract : GroupOfContract
{
    [Required]
    [NotVisible]
    [Display(Name = "InsurancePortfolio")]
    [Dimension(typeof(InsurancePortfolio))]
    //[Immutable]
    public string Portfolio
    {
        get => base.Portfolio;
        init => base.Portfolio = value;
    }

    // TODO: for the case of internal reinsurance the Partner would be the reporting node, hence not null.
    // If this is true we need the [Required] attribute here, add some validation at dataNode import 
    // and to add logic in the GetNonPerformanceRiskRate method in ImportStorage.
    [NotVisible]
    [NotMapped]
    //[Immutable]
    public override string Partner => null;
}