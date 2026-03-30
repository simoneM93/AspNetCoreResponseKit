using AspNetCoreResponseKit.Mapping;
using AspNetCoreResponseKit.Models;
using AspNetCoreResponseKit.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Middleware
{
    /// <summary>
    /// Global exception handling middleware.
    /// Catches all unhandled exceptions and converts them to a standardized ApiResponse.
    /// Register with app.UseApiExceptionHandler().
    /// </summary>
    public sealed class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ExceptionMapperResolver _resolver;
        private readonly ResponseKitOptions _options;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ApiExceptionMiddleware(
            RequestDelegate next,
            ResponseKitOptions options,
            ILogger<ApiExceptionMiddleware> logger)
        {
            ArgumentNullException.ThrowIfNull(next);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _next = next;
            _options = options;
            _resolver = new ExceptionMapperResolver(options);
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Client disconnected — log at debug level, no response needed
                _logger.LogDebug("Request cancelled by client: {Path}", context.Request.Path);
                context.Response.StatusCode = 499;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = _resolver.Resolve(exception);

            _logger.LogError(exception,
                "Unhandled exception on {Method} {Path} — {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                (int)statusCode);

            var errors = new List<ApiError>();

            if (_options.IncludeExceptionDetails)
            {
                errors.Add(new ApiError(exception.Message, exception.GetType().Name));
                if (exception.StackTrace is not null)
                    errors.Add(new ApiError(exception.StackTrace, "StackTrace"));
            }

            var response = ApiResponse.Error<object>(message, statusCode);

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(
                new
                {
                    isSuccess = false,
                    data = (object?)null,
                    message,
                    errors = errors.Select(e => new { e.Message, e.Code }),
                    statusCode = (int)statusCode
                },
                _jsonOptions);

            await context.Response.WriteAsync(json);
        }
    }
}
