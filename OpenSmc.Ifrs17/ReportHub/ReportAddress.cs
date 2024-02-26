using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReportHub;

public record ReportAddress(object Host) : IHostedAddress;
