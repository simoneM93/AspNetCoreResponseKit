using AspNetCoreAuthKit.Tokens.Exceptions;
using AspNetCoreHttpKit.Models;
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
    /// Maps exceptions from AspNetCoreAuthKit and AspNetCoreHttpKit
    /// to the appropriate HTTP status codes.
    /// Applied before DefaultExceptionMapper in the resolution chain.
    /// </summary>
    internal sealed class AuthKitExceptionMapper : IExceptionMapper
    {
        public bool CanHandle(Exception exception)
            => exception is HttpServiceException or RefreshTokenException;

        public (HttpStatusCode StatusCode, string Message) Map(Exception exception)
            => exception switch
            {
                // AspNetCoreHttpKit typed exceptions
                HttpBadRequestException ex => (HttpStatusCode.BadRequest, ex.Message),
                HttpUnauthorizedException ex => (HttpStatusCode.Unauthorized, ex.Message),
                HttpForbiddenException ex => (HttpStatusCode.Forbidden, ex.Message),
                HttpNotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
                HttpConflictException ex => (HttpStatusCode.Conflict, ex.Message),
                HttpUnprocessableEntityException ex => (HttpStatusCode.UnprocessableEntity, ex.Message),
                HttpTooManyRequestsException ex => (HttpStatusCode.TooManyRequests, ex.Message),
                HttpServerErrorException ex => (ex.StatusCode, ex.Message),
                HttpServiceException ex => (ex.StatusCode, ex.Message),

                // AspNetCoreAuthKit refresh token exceptions
                RefreshTokenException ex => (HttpStatusCode.Unauthorized, ex.Message),

                _ => (HttpStatusCode.InternalServerError, ResponseKitDefaults.DefaultErrorMessage)
            };
    }
}
