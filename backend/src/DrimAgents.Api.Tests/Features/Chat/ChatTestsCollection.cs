using DrimAgents.Api.Tests.Fixtures;

namespace DrimAgents.Api.Tests.Features.Chat;

[CollectionDefinition(Name)]
public class ChatTestsCollection : ICollectionFixture<TestFixture>
{
    public const string Name = nameof(ChatTestsCollection);
}
