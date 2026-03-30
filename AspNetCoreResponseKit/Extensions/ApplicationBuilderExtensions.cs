using AspNetCoreResponseKit.Middleware;
using AspNetCoreResponseKit.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AspNetCoreResponseKit.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Registers the global API exception handler middleware.
        /// Must be called before app.UseRouting() and app.UseAuthentication().
        ///
        /// Usage:
        ///   app.UseApiExceptionHandler();
        /// </summary>
        public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            var options = app.ApplicationServices.GetService<ResponseKitOptions>()
                ?? new ResponseKitOptions();

            return app.UseMiddleware<ApiExceptionMiddleware>(options);
        }
    }
}
