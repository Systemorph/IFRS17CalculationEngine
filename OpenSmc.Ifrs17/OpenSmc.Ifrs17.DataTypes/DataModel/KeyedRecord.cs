using System.ComponentModel.DataAnnotations;
using OpenSmc.Domain.Abstractions.Attributes;
using OpenSmc.Ifrs17.DataTypes.DataModel.Interfaces;

namespace OpenSmc.Ifrs17.DataTypes.DataModel;

public abstract record KeyedRecord : IKeyed
{
    [Key][NotVisible] public Guid Id { get; init; }
}