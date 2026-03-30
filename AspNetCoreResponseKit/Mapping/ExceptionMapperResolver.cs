using AspNetCoreResponseKit.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Mapping
{
    /// <summary>
    /// Resolves the correct exception mapper following the priority chain:
    ///   1. Custom mappings registered via ResponseKitOptions.MapException&lt;T&gt;
    ///   2. AuthKitExceptionMapper (AspNetCoreAuthKit + AspNetCoreHttpKit exceptions)
    ///   3. DefaultExceptionMapper (.NET standard exceptions)
    ///   4. Fallback 500
    /// </summary>
    internal sealed class ExceptionMapperResolver
    {
        private readonly ResponseKitOptions _options;
        private readonly AuthKitExceptionMapper _authKitMapper = new();
        private readonly DefaultExceptionMapper _defaultMapper = new();

        public ExceptionMapperResolver(ResponseKitOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options;
        }

        public (HttpStatusCode StatusCode, string Message) Resolve(Exception exception)
        {
            // 1. Custom mappings — exact type match first, then base types
            var exceptionType = exception.GetType();
            while (exceptionType is not null)
            {
                if (_options.ExceptionMappings.TryGetValue(exceptionType, out var customMapper))
                    return customMapper(exception);

                exceptionType = exceptionType.BaseType;
            }

            // 2. AuthKit mapper
            if (_authKitMapper.CanHandle(exception))
                return _authKitMapper.Map(exception);

            // 3. Default mapper
            if (_defaultMapper.CanHandle(exception))
                return _defaultMapper.Map(exception);

            // 4. Fallback
            return (HttpStatusCode.InternalServerError, ResponseKitDefaults.DefaultErrorMessage);
        }
    }
}
