using DrimAgents.Api.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using DrimAgents.Api.Tests.Harnesses;

namespace DrimAgents.Api.Tests.Fixtures;

public class TestFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public TestFixture()
    {
        Database = new DatabaseHarness<Program, AppDbContext>("drimagentsdb");
        HttpClient = new HttpClientHarness<Program>();

        _factory = new WebApplicationFactory<Program>()
            .AddHarness(Database)
            .AddHarness(HttpClient);
    }

    public WebApplicationFactory<Program> Factory => _factory;
    public DatabaseHarness<Program, AppDbContext> Database { get; }
    public HttpClientHarness<Program> HttpClient { get; }

    public async Task Reset(CancellationToken cancellationToken)
    {
        await Database.Clear(cancellationToken);
    }

    private static CancellationToken CreateCancellationToken(int timeoutSeconds = 30)
    {
        return new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)).Token;
    }

    public async Task InitializeAsync()
    {
        await Database.Start(_factory, CreateCancellationToken(60));
        await Database.Migrate(CreateCancellationToken(60));
        await HttpClient.Start(_factory, CreateCancellationToken());

        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
        await HttpClient.Stop(CreateCancellationToken());
        await Database.Stop(CreateCancellationToken());
    }
}
