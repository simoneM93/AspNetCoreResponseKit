using System.Net;

namespace AspNetCoreResponseKit
{
    /// <summary>
    /// Internal runtime defaults — set by ResponseKitOptions at startup.
    /// </summary>
    internal static class ResponseKitDefaults
    {
        internal static HttpStatusCode NullStatusCode { get; set; } = HttpStatusCode.NotFound;
        internal static string NullMessage { get; set; } = "Resource not found.";
        internal static bool PropagateUpstreamStatusCodes { get; set; } = true;
        internal static bool IncludeExceptionDetails { get; set; } = false;
        internal static string DefaultErrorMessage { get; set; } = "An unexpected error occurred.";
    }
}
