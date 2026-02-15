namespace DrimAgents.Api.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string resourceType, object id)
        : base($"{resourceType} with ID '{id}' was not found.")
    {
    }
}

public class ForbiddenException : Exception
{
    public string? ErrorCode { get; }

    public ForbiddenException(string message) : base(message)
    {
    }

    public ForbiddenException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public ForbiddenException() : base("You do not have permission to perform this action.")
    {
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }
}

public class UnprocessableEntityException : Exception
{
    public UnprocessableEntityException(string message) : base(message)
    {
    }
}
