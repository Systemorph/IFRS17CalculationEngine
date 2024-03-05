using OpenSmc.Messaging;

namespace OpenSmc.Ifrs17.IfrsVariableHub;

public record IfrsVariableAddress(object Host, int Year, int Month, string ReportingNode, string Scenario) : IHostedAddress;


