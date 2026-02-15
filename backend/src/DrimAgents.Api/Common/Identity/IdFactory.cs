using IdGen;

namespace DrimAgents.Api.Common.Identity;

public interface IIdFactory
{
    long CreateId();
}

public class IdFactory : IIdFactory
{
    private readonly IdGenerator _generator;

    public IdFactory(IdGenerator generator)
    {
        _generator = generator;
    }

    public long CreateId()
    {
        return _generator.CreateId();
    }
}
