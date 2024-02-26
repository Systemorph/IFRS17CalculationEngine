using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ParameterDataHub;

public record ParameterAddress(object Host) : IHostedAddress;