using System.Collections.Concurrent;
using DrimAgents.Api.Common.AgentGateway;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DrimAgents.Api.Tests.Harnesses;

public class AgentGatewayHarness<TProgram> : IHarness<TProgram>
    where TProgram : class
{
    private readonly MockAgentGateway _mock = new();

    public bool IsAvailable
    {
        get => _mock.IsAvailable;
        set => _mock.IsAvailable = value;
    }

    public IReadOnlyList<SentMessage> GetSentMessages() => _mock.GetSentMessages();

    public void Reset()
    {
        _mock.IsAvailable = true;
        _mock.Reset();
    }

    public void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IAgentGateway>(_mock);
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

    public record SentMessage(string TaskId, string ChatSessionId, string? ClaudeSessionId, string Content);

    private class MockAgentGateway : IAgentGateway
    {
        private readonly ConcurrentBag<SentMessage> _sentMessages = [];

        public bool IsAvailable { get; set; } = true;

        public Task SendMessageAsync(string taskId, string chatSessionId, string? claudeSessionId, string content, CancellationToken ct)
        {
            _sentMessages.Add(new SentMessage(taskId, chatSessionId, claudeSessionId, content));
            return Task.CompletedTask;
        }

        public IReadOnlyList<SentMessage> GetSentMessages() => _sentMessages.ToList();

        public void Reset() => _sentMessages.Clear();
    }
}
