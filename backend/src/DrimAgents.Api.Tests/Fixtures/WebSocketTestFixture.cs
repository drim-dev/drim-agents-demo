using DrimAgents.Api.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using DrimAgents.Api.Tests.Harnesses;

namespace DrimAgents.Api.Tests.Fixtures;

public class WebSocketTestFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebSocketTestFixture()
    {
        Database = new DatabaseHarness<Program, AppDbContext>("drimagentsdb");
        HttpServer = new HttpServerHarness<Program>();
        HttpClient = new HttpClientHarness<Program>();

        _factory = new WebApplicationFactory<Program>()
            .AddHarness(Database)
            .AddHarness(HttpServer)
            .AddHarness(HttpClient)
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("AgentGateway:ApiKey", "test-api-key");
            });
    }

    public WebApplicationFactory<Program> Factory => _factory;
    public DatabaseHarness<Program, AppDbContext> Database { get; }
    public HttpServerHarness<Program> HttpServer { get; }
    public HttpClientHarness<Program> HttpClient { get; }

    public async Task Reset(CancellationToken cancellationToken)
    {
        await Database.Clear(cancellationToken);
        HttpServer.Reset();
    }

    private static CancellationToken CreateCancellationToken(int timeoutSeconds = 30)
    {
        return new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;
    }

    public async Task InitializeAsync()
    {
        await Database.Start(_factory, CreateCancellationToken(60));
        await Database.Migrate(CreateCancellationToken(60));
        await HttpServer.Start(_factory, CreateCancellationToken());
        await HttpClient.Start(_factory, CreateCancellationToken());

        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
        await HttpClient.Stop(CreateCancellationToken());
        await HttpServer.Stop(CreateCancellationToken());
        await Database.Stop(CreateCancellationToken());
    }
}
