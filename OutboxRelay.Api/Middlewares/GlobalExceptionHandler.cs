using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OutboxRelay.Common.Exceptions;
using System.Net;

namespace OutboxRelay.Api.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var mapping = MapException(exception);

            _logger.LogError(
                exception,
                "Exception occurred: {Message}. StatusCode: {StatusCode}",
                exception.Message,
                mapping.StatusCode);

            var problemDetails = new ProblemDetails
            {
                Status = mapping.StatusCode,
                Title = mapping.Title,
                Detail = mapping.Detail,
                Instance = httpContext.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["traceId"] = httpContext.TraceIdentifier,
                    ["timestamp"] = DateTimeOffset.UtcNow
                }
            };

            if (exception is OutboxException outboxException)
            {
                problemDetails.Type = outboxException.GetType().Name;
            }
            else if (exception is TransactionException transactionException)
            {
                problemDetails.Type = transactionException.GetType().Name;
            }

            httpContext.Response.StatusCode = mapping.StatusCode;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        private static ExceptionMapping MapException(Exception exception)
        {
            return exception switch
            {
                OutboxNotFoundException => new ExceptionMapping(
                (int)HttpStatusCode.NotFound,
                "Outbox Not Found",
                exception.Message),

                TransactionNotFoundException => new ExceptionMapping(
                (int)HttpStatusCode.NotFound,
                "Transaction Not Found",
                exception.Message),

                ArgumentException => new ExceptionMapping(
                (int)HttpStatusCode.BadRequest,
                "Invalid Argument",
                exception.Message),

                InvalidOperationException => new ExceptionMapping(
                (int)HttpStatusCode.Forbidden,
                "Invalid Operation",
                "An error occurred while processing your request."),

                _ => new ExceptionMapping(
               (int)HttpStatusCode.InternalServerError,
               "An error occurred",
               "An unexpected error occurred while processing your request.")
            };
        }

        private readonly record struct ExceptionMapping(int StatusCode, string Title, string Detail);
    }
}
