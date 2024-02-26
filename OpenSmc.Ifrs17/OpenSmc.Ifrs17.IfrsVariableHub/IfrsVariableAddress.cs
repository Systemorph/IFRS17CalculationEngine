using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.IfrsVariableHub;

public record IfrsVariableAddress(object Host) : IHostedAddress;
