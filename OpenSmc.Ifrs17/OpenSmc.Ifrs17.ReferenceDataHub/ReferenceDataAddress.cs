using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReferenceDataHub;

public record ReferenceDataAddress(object Host) : IHostedAddress;

public record ReferenceDataImportAddress(object Host) : IHostedAddress;

