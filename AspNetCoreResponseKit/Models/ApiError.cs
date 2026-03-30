using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Models
{
    /// <summary>
    /// Represents a single error detail within an <see cref="ApiResponse{T}"/>.
    /// </summary>
    public sealed record ApiError(string Message, string? Code = null);
}
