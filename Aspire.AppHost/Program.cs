var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("drim-agents-data")
    .WithPgAdmin()
    .AddDatabase("drimagentsdb");

var api = builder.AddProject<Projects.DrimAgents_Api>("api")
    .WithReference(postgres)
    .WaitFor(postgres);

var frontend = builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithReference(api)
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
