using DrimAgents.Api.Tests.Fixtures;

namespace DrimAgents.Api.Tests.Features.Chat;

[CollectionDefinition(Name)]
public class WebSocketTestsCollection : ICollectionFixture<WebSocketTestFixture>
{
    public const string Name = nameof(WebSocketTestsCollection);
}
