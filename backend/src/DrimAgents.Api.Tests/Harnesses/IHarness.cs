using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DrimAgents.Api.Tests.Harnesses;

public interface IHarness<T> where T : class
{
    void ConfigureWebHostBuilder(IWebHostBuilder builder);

    Task Start(WebApplicationFactory<T> factory, CancellationToken cancellationToken);

    Task Stop(CancellationToken cancellationToken);
}
