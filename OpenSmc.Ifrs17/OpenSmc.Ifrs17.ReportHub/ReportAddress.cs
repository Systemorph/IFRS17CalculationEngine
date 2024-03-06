using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.ReportHub;

public record ReportAddress(object Host, int Year, int Month, string ReportingNode, string Scenario) : IHostedAddress;
