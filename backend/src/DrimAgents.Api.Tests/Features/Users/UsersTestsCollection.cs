using DrimAgents.Api.Tests.Fixtures;

namespace DrimAgents.Api.Tests.Features.Users;

[CollectionDefinition(Name)]
public class UsersTestsCollection : ICollectionFixture<TestFixture>
{
    public const string Name = nameof(UsersTestsCollection);
}
