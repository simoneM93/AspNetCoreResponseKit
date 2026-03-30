using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Options
{
    /// <summary>
    /// Configuration options for AspNetCoreResponseKit.
    /// </summary>
    public sealed class ResponseKitOptions
    {
        /// <summary>
        /// HTTP status code returned when a smart factory (From/FromAsync) receives a null value.
        /// Default: 404 Not Found.
        /// </summary>
        public HttpStatusCode NullResponseStatusCode { get; set; } = HttpStatusCode.NotFound;

        /// <summary>
        /// Default message used when a null value is returned by a smart factory.
        /// Default: "Resource not found."
        /// </summary>
        public string NullResponseMessage { get; set; } = "Resource not found.";

        /// <summary>
        /// When true, the status code from an upstream HTTP call (via AspNetCoreHttpKit)
        /// is propagated as-is in the ApiResponse. When false, upstream errors are always
        /// mapped to 500 Internal Server Error.
        /// Default: true.
        /// </summary>
        public bool PropagateUpstreamStatusCodes { get; set; } = true;

        /// <summary>
        /// When true, exception details (message and stack trace) are included
        /// in the ApiResponse errors. Should be false in production.
        /// Default: false.
        /// </summary>
        public bool IncludeExceptionDetails { get; set; } = false;

        /// <summary>
        /// Default error message used when an unhandled exception is caught
        /// and IncludeExceptionDetails is false.
        /// Default: "An unexpected error occurred."
        /// </summary>
        public string DefaultErrorMessage { get; set; } = "An unexpected error occurred.";

        /// <summary>
        /// Custom exception mappings registered by the user.
        /// Key = exception type, Value = factory returning (statusCode, message).
        /// </summary>
        internal Dictionary<Type, Func<Exception, (HttpStatusCode StatusCode, string Message)>> ExceptionMappings { get; } = [];

        /// <summary>
        /// Registers a custom exception mapping.
        /// </summary>
        public ResponseKitOptions MapException<TException>(
            Func<TException, (HttpStatusCode StatusCode, string Message)> mapper)
            where TException : Exception
        {
            ExceptionMappings[typeof(TException)] = ex => mapper((TException)ex);
            return this;
        }
    }
}
