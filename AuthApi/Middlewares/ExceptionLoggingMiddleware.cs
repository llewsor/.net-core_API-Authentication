using AuthApi.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Middlewares
{
    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionLoggingMiddleware> _logger;

        public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (InvalidCredentialsException ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started, cannot modify for {Path}", context.Request.Path);
                    throw;
                }

                _logger.LogWarning(ex, "Authentication failed for {Path}", context.Request.Path);
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.Append("WWW-Authenticate", "Bearer realm=\"AuthApi\"");
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Type = "https://yourapi.com/docs/errors/invalid-credentials",
                    Title = "Authentication failed",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Username or password is incorrect.",
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problem);
            }

            catch (UsernameInUseException ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started, cannot modify for {Path}", context.Request.Path);
                    throw;
                }

                _logger.LogWarning(ex, "Username already in use {Path}", context.Request.Path);
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Type = "https://yourapi.com/docs/errors/username-inuse",
                    Title = "Authentication failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Username already in use.",
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problem);
            }
            catch (RefreshTokenExpiredException ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started, cannot modify for {Path}", context.Request.Path);
                    throw;
                }

                _logger.LogWarning(ex, "Refresh token expired for {Path}", context.Request.Path);
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Type = "https://yourapi.com/docs/errors/refresh-token-expired",
                    Title = "Session expired",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Your session has expired. Please log in again.",
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problem);
            }
            catch (UserBlockedException ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started, cannot modify for {Path}", context.Request.Path);
                    throw;
                }

                _logger.LogWarning(ex, "Blocked user tried to access {Path}", context.Request.Path);
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Type = "https://yourapi.com/docs/errors/account-blocked",
                    Title = "Account blocked",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = "Your account has been blocked. Please contact support.",
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problem);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogError("Response already started, cannot modify for {Path}", context.Request.Path);
                    throw;
                }

                _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Type = "https://yourapi.com/docs/errors/internal-server-error",
                    Title = "Internal server error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An unexpected error occurred. Please try again later.",
                    Instance = context.Request.Path
                };

                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }
}
