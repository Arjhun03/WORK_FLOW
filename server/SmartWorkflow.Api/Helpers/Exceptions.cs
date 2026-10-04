namespace SmartWorkflow.Api.Helpers;

public class AppException : Exception
{
    public int StatusCode { get; }
    public List<string> Errors { get; }

    public AppException(string message, int statusCode = 400, List<string>? errors = null) 
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new List<string>();
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) 
        : base(message, 404)
    {
    }
}

public class BadRequestException : AppException
{
    public BadRequestException(string message, List<string>? errors = null) 
        : base(message, 400, errors)
    {
    }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "You are not authenticated.") 
        : base(message, 401)
    {
    }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You are not authorized to perform this action.") 
        : base(message, 403)
    {
    }
}

public class InvalidStateTransitionException : AppException
{
    public InvalidStateTransitionException(string currentState, string targetState) 
        : base($"Invalid state transition from '{currentState}' to '{targetState}'.", 400)
    {
    }
}
