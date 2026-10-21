namespace ServiceDesk.Domain.Exceptions;

/// <summary>A business rule was violated (mapped to HTTP 422).</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>The requested record does not exist (mapped to HTTP 404).</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with id '{key}' was not found.") { }
}

/// <summary>The request conflicts with current state, e.g. duplicate name (mapped to HTTP 409).</summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>The user is signed in but not allowed to do this (mapped to HTTP 403).</summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}
