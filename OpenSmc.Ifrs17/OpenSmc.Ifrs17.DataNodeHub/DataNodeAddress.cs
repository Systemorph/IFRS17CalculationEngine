using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.DataNodeHub;

public record DataNodeAddress(object Host) : IHostedAddress;
