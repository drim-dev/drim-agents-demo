using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace DrimAgents.Api.Tests.Harnesses;

public class HttpServerHarness<TProgram> : IHarness<TProgram>
    where TProgram : class
{
    private readonly ConcurrentDictionary<string, MockClientConfig> _clients = new();
    private readonly ConcurrentBag<RecordedRequest> _recordedRequests = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public MockClientBuilder ForClient(string clientName)
    {
        var config = _clients.GetOrAdd(clientName, _ => new MockClientConfig());
        return new MockClientBuilder(config);
    }

    public void Reset()
    {
        _clients.Clear();
        _recordedRequests.Clear();
    }

    public IReadOnlyList<RecordedRequest> GetRecordedRequests(string? clientName = null)
    {
        var requests = _recordedRequests.AsEnumerable();
        if (clientName is not null)
            requests = requests.Where(r => r.ClientName == clientName);
        return requests.ToList();
    }

    public bool WasRequested(string clientName, HttpMethod method, string path)
    {
        return _recordedRequests.Any(r =>
            r.ClientName == clientName &&
            r.Method == method &&
            r.Path == path);
    }

    public void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                {
                    var clientName = handlerBuilder.Name;
                    if (clientName is not null)
                    {
                        handlerBuilder.PrimaryHandler = new MockHttpMessageHandler(clientName, _clients, _recordedRequests);
                    }
                });
            });
        });
    }

    public Task Start(WebApplicationFactory<TProgram> factory, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Stop(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public class MockClientBuilder
    {
        private readonly MockClientConfig _config;

        internal MockClientBuilder(MockClientConfig config)
        {
            _config = config;
        }

        public MockResponseBuilder RespondTo(HttpMethod method, string path)
        {
            var key = new RequestKey(method, path);
            var response = new MockResponse();
            _config.Responses[key] = response;
            return new MockResponseBuilder(response);
        }
    }

    public class MockResponseBuilder
    {
        private readonly MockResponse _response;

        internal MockResponseBuilder(MockResponse response)
        {
            _response = response;
        }

        public MockResponseBuilder WithJson(object body)
        {
            _response.StatusCode = HttpStatusCode.OK;
            _response.Body = JsonSerializer.Serialize(body, JsonOptions);
            _response.ContentType = "application/json";
            return this;
        }

        public MockResponseBuilder WithStatusCode(HttpStatusCode statusCode)
        {
            _response.StatusCode = statusCode;
            return this;
        }

        public MockResponseBuilder WithError()
        {
            _response.SimulateNetworkError = true;
            return this;
        }
    }

    internal record RequestKey(HttpMethod Method, string Path);

    internal class MockResponse
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string? Body { get; set; }
        public string? ContentType { get; set; }
        public bool SimulateNetworkError { get; set; }
    }

    internal class MockClientConfig
    {
        public ConcurrentDictionary<RequestKey, MockResponse> Responses { get; } = new();
    }

    public record RecordedRequest(
        string ClientName,
        HttpMethod Method,
        string Path,
        Dictionary<string, string[]> Headers);

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _clientName;
        private readonly ConcurrentDictionary<string, MockClientConfig> _clients;
        private readonly ConcurrentBag<RecordedRequest> _recordedRequests;

        public MockHttpMessageHandler(
            string clientName,
            ConcurrentDictionary<string, MockClientConfig> clients,
            ConcurrentBag<RecordedRequest> recordedRequests)
        {
            _clientName = clientName;
            _clients = clients;
            _recordedRequests = recordedRequests;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "/";
            var headers = request.Headers
                .ToDictionary(h => h.Key, h => h.Value.ToArray());

            _recordedRequests.Add(new RecordedRequest(
                _clientName,
                request.Method,
                path,
                headers));

            var key = new RequestKey(request.Method, path);

            if (!_clients.TryGetValue(_clientName, out var config) ||
                !config.Responses.TryGetValue(key, out var mockResponse))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent($"No mock configured for {request.Method} {path}")
                });
            }

            if (mockResponse.SimulateNetworkError)
            {
                throw new HttpRequestException("Simulated network error");
            }

            var response = new HttpResponseMessage(mockResponse.StatusCode);
            if (mockResponse.Body is not null)
            {
                response.Content = new StringContent(
                    mockResponse.Body,
                    System.Text.Encoding.UTF8,
                    mockResponse.ContentType ?? "application/json");
            }

            return Task.FromResult(response);
        }
    }
}
