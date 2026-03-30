using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Mapping.Interfaces
{
    /// <summary>
    /// Maps an exception to an HTTP status code and message.
    /// Implement this interface to provide custom exception mapping logic.
    /// </summary>
    public interface IExceptionMapper
    {
        /// <summary>
        /// Returns true if this mapper can handle the given exception type.
        /// </summary>
        bool CanHandle(Exception exception);

        /// <summary>
        /// Maps the exception to a status code and error message.
        /// </summary>
        (HttpStatusCode StatusCode, string Message) Map(Exception exception);
    }
}
