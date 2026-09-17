namespace BloodConnect.Api;

/// <summary>
/// Thrown for client input/state validation failures that should be surfaced as HTTP 400 with a
/// clear message. Used instead of broad catch blocks — controllers let this propagate to a single
/// exception-handling middleware registered in Program.cs.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when the caller is authenticated but not authorized to act on the requested resource
/// (e.g. editing another user's profile). Mapped to HTTP 403.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when the requested resource does not exist or is not visible to the caller. Mapped to HTTP 404.
/// </summary>
public class NotFoundApiException : Exception
{
    public NotFoundApiException(string message) : base(message)
    {
    }
}
