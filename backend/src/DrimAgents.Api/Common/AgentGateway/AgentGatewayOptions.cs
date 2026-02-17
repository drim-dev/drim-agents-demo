namespace DrimAgents.Api.Common.AgentGateway;

public class AgentGatewayOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public int StreamStartTimeoutSeconds { get; set; } = 30;
    public int StreamCompleteTimeoutSeconds { get; set; } = 300;
}
