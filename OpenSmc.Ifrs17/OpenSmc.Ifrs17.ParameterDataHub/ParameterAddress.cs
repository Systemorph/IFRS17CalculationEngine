using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ParameterDataHub;

public record ParameterDataAddress(object Host) : IHostedAddress;
public record ParameterImportAddress(object Host) : IHostedAddress;
