using System.ComponentModel.DataAnnotations;
using OpenSmc.Ifrs17.Domain.DataModel.Interfaces;
using Systemorph.Vertex.Api.Attributes;

namespace OpenSmc.Ifrs17.Domain.DataModel;

public abstract record KeyedRecord : IKeyed
{
    [Key] [NotVisible] public Guid Id { get; init; }
}