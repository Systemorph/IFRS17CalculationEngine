using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public record ReadManyCurrencyRequest : IRequest<IReadOnlyCollection<Currency>>;

public record ReadCurrencyRequest : IRequest<Currency>;