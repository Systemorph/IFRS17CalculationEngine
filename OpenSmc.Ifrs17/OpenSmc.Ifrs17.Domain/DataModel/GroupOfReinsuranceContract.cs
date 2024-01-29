using System.ComponentModel.DataAnnotations;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public record GroupOfReinsuranceContract : GroupOfContract
{
    [Required]
    [NotVisible]
    [Display(Name = "ReinsurancePortfolio")]
    [Dimension(typeof(ReinsurancePortfolio))]
    //[Immutable]
    public string Portfolio
    {
        get => base.Portfolio;
        init => base.Portfolio = value;
    }
}