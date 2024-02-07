using OpenSmc.Ifrs17.Domain.DataModel.FinancialDataDimensions;
using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public record  ReadManyLobRequest : IRequest<IReadOnlyCollection<LineOfBusiness>>;

public record ReadLobRequest : IRequest<LineOfBusiness>;