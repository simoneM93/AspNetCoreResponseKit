using AspNetCoreResponseKit.Mapping.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Mapping
{
    /// <summary>
    /// Maps standard .NET exceptions to HTTP status codes.
    /// Applied when no custom mapper and no AuthKit mapper matches.
    /// </summary>
    internal sealed class DefaultExceptionMapper : IExceptionMapper
    {
        public bool CanHandle(Exception exception) => true; // fallback — handles everything

        public (HttpStatusCode StatusCode, string Message) Map(Exception exception)
            => exception switch
            {
                ArgumentNullException ex => (HttpStatusCode.BadRequest, ex.Message),
                ArgumentException ex => (HttpStatusCode.BadRequest, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized."),
                NotImplementedException => (HttpStatusCode.NotImplemented, "Not implemented."),
                OperationCanceledException => ((HttpStatusCode)499, "Request cancelled."),
                TimeoutException => (HttpStatusCode.RequestTimeout, "The request timed out."),
                InvalidOperationException ex => (HttpStatusCode.Conflict, ex.Message),
                KeyNotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
                _ => (HttpStatusCode.InternalServerError, ResponseKitDefaults.DefaultErrorMessage)
            };
    }
}
