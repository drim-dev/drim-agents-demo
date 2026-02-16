using DrimAgents.Api.Tests.Fixtures;

namespace DrimAgents.Api.Tests.Features.Projects;

[CollectionDefinition(Name)]
public class ProjectsTestsCollection : ICollectionFixture<TestFixture>
{
    public const string Name = nameof(ProjectsTestsCollection);
}
