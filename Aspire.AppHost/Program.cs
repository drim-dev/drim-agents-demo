var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("drim-agents-data")
    .WithPgAdmin()
    .AddDatabase("drimagentsdb");

var agentApiKey = builder.AddParameter("AgentApiKey", secret: true);

var api = builder.AddProject<Projects.DrimAgents_Api>("api")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("AgentGateway__ApiKey", agentApiKey);

var agentDaemon = builder.AddNpmApp("agent-daemon", "../agent-daemon", "start")
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("BACKEND_WS_URL", ReferenceExpression.Create(
        $"ws://{api.GetEndpoint("http").Property(EndpointProperty.Host)}:{api.GetEndpoint("http").Property(EndpointProperty.Port)}/ws/agent"))
    .WithEnvironment("AGENT_API_KEY", agentApiKey);

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
