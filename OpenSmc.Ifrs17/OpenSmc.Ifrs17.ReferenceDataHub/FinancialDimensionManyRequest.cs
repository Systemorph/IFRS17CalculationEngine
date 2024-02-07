using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public record FinancialDimensionManyRequest<TDim> : IRequest<IReadOnlyCollection<TDim>>;