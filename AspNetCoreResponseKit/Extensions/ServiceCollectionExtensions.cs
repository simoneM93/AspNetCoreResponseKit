using AspNetCoreResponseKit.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers AspNetCoreResponseKit services.
        /// </summary>
        public static IServiceCollection AddAspNetCoreResponseKit(
            this IServiceCollection services,
            Action<ResponseKitOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var options = new ResponseKitOptions();
            configure?.Invoke(options);

            // Apply options to runtime defaults used by ApiResponse static methods
            ResponseKitDefaults.NullStatusCode = options.NullResponseStatusCode;
            ResponseKitDefaults.NullMessage = options.NullResponseMessage;
            ResponseKitDefaults.PropagateUpstreamStatusCodes = options.PropagateUpstreamStatusCodes;
            ResponseKitDefaults.IncludeExceptionDetails = options.IncludeExceptionDetails;
            ResponseKitDefaults.DefaultErrorMessage = options.DefaultErrorMessage;

            // Register options as singleton for middleware consumption
            services.AddSingleton(options);

            return services;
        }
    }
}
