using AspNetCoreHttpKit.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Models
{
    /// <summary>
    /// Standardized API response envelope.
    /// </summary>
    public sealed class ApiResponse<T>
    {
        public bool IsSuccess { get; }
        public T? Data { get; }
        public string? Message { get; }
        public IReadOnlyList<ApiError> Errors { get; }
        public HttpStatusCode StatusCode { get; }

        private ApiResponse(
            bool isSuccess,
            T? data,
            HttpStatusCode statusCode,
            string? message,
            IReadOnlyList<ApiError>? errors)
        {
            IsSuccess = isSuccess;
            Data = data;
            StatusCode = statusCode;
            Message = message;
            Errors = errors ?? [];
        }

        internal static ApiResponse<T> Success(T? data, HttpStatusCode statusCode, string? message = null)
            => new(true, data, statusCode, message, null);

        internal static ApiResponse<T> Failure(HttpStatusCode statusCode, string message, IReadOnlyList<ApiError>? errors = null)
            => new(false, default, statusCode, message, errors);

        // ----------------------------------------------------------
        // Fluent transformations
        // ----------------------------------------------------------

        /// <summary>
        /// Transforms the data when the response is successful.
        /// </summary>
        public ApiResponse<TOut> OnSuccess<TOut>(Func<T?, TOut> transform)
        {
            if (!IsSuccess)
                return ApiResponse<TOut>.Failure(StatusCode, Message ?? string.Empty, Errors.ToList());

            return ApiResponse<TOut>.Success(transform(Data), StatusCode, Message);
        }

        /// <summary>
        /// Overrides the message and status code when the data is null.
        /// </summary>
        public ApiResponse<T> OnNull(string message, HttpStatusCode statusCode = HttpStatusCode.NotFound)
        {
            if (IsSuccess && Data is null)
                return Failure(statusCode, message);

            return this;
        }

        /// <summary>
        /// Converts to an ASP.NET Core IResult for use in Minimal APIs.
        /// </summary>
        public IResult ToResult()
            => Results.Json(this, statusCode: (int)StatusCode);
    }

    /// <summary>
    /// Non-generic version for operations without a response body (e.g. DELETE).
    /// </summary>
    public sealed class ApiResponse
    {
        public bool IsSuccess { get; }
        public string? Message { get; }
        public IReadOnlyList<ApiError> Errors { get; }
        public HttpStatusCode StatusCode { get; }

        private ApiResponse(bool isSuccess, HttpStatusCode statusCode, string? message, IReadOnlyList<ApiError>? errors)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Message = message;
            Errors = errors ?? [];
        }

        // ----------------------------------------------------------
        // Explicit factory methods
        // ----------------------------------------------------------

        public static ApiResponse<T> Ok<T>(T data, string? message = null)
            => ApiResponse<T>.Success(data, HttpStatusCode.OK, message);

        public static ApiResponse<T> Created<T>(T data, string? message = null)
            => ApiResponse<T>.Success(data, HttpStatusCode.Created, message);

        public static ApiResponse Ok(string? message = null)
            => new(true, HttpStatusCode.OK, message, null);

        public static ApiResponse NoContent()
            => new(true, HttpStatusCode.NoContent, null, null);

        public static ApiResponse<T> NotFound<T>(string message = "Resource not found.")
            => ApiResponse<T>.Failure(HttpStatusCode.NotFound, message);

        public static ApiResponse<T> BadRequest<T>(string message, params ApiError[] errors)
            => ApiResponse<T>.Failure(HttpStatusCode.BadRequest, message, errors);

        public static ApiResponse<T> Unauthorized<T>(string message = "Unauthorized.")
            => ApiResponse<T>.Failure(HttpStatusCode.Unauthorized, message);

        public static ApiResponse<T> Forbidden<T>(string message = "Forbidden.")
            => ApiResponse<T>.Failure(HttpStatusCode.Forbidden, message);

        public static ApiResponse<T> Conflict<T>(string message)
            => ApiResponse<T>.Failure(HttpStatusCode.Conflict, message);

        public static ApiResponse<T> Error<T>(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            => ApiResponse<T>.Failure(statusCode, message);

        public static ApiResponse Error(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            => new(false, statusCode, message, null);

        // ----------------------------------------------------------
        // Smart factory — sync
        // ----------------------------------------------------------

        /// <summary>
        /// Returns Ok if value is not null, otherwise returns the configured null status code.
        /// </summary>
        public static ApiResponse<T> From<T>(T? value, string? nullMessage = null, HttpStatusCode? nullStatusCode = null)
        {
            if (value is null)
            {
                var code = nullStatusCode ?? ResponseKitDefaults.NullStatusCode;
                return ApiResponse<T>.Failure(code, nullMessage ?? ResponseKitDefaults.NullMessage);
            }

            return ApiResponse<T>.Success(value, HttpStatusCode.OK);
        }

        /// <summary>
        /// Returns Ok if true, otherwise returns the given error.
        /// </summary>
        public static ApiResponse FromBool(bool success, string onTrue = "Success.", string onFalse = "Operation failed.", HttpStatusCode failureCode = HttpStatusCode.BadRequest)
            => success
                ? new(true, HttpStatusCode.OK, onTrue, null)
                : new(false, failureCode, onFalse, null);

        // ----------------------------------------------------------
        // Smart factory — async
        // ----------------------------------------------------------

        /// <summary>
        /// Executes the factory and returns Ok if the result is not null.
        /// Exceptions are NOT caught here — let the middleware handle them.
        /// </summary>
        public static async Task<ApiResponse<T>> FromAsync<T>(
            Func<Task<T?>> factory,
            string? nullMessage = null,
            HttpStatusCode? nullStatusCode = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = await factory();
            return From(value, nullMessage, nullStatusCode);
        }

        // ----------------------------------------------------------
        // Integration with AspNetCoreHttpKit
        // ----------------------------------------------------------

        /// <summary>
        /// Converts an HttpResult&lt;T&gt; from AspNetCoreHttpKit to an ApiResponse&lt;T&gt;.
        /// Status code propagation follows ResponseKitOptions.PropagateUpstreamStatusCodes.
        /// </summary>
        public static ApiResponse<T> From<T>(HttpResult<T> result)
        {
            if (result.IsSuccess)
                return ApiResponse<T>.Success(result.Data, result.StatusCode);

            var statusCode = ResponseKitDefaults.PropagateUpstreamStatusCodes
                ? result.StatusCode
                : HttpStatusCode.InternalServerError;

            return ApiResponse<T>.Failure(statusCode, result.ErrorMessage ?? "Upstream error.");
        }

        /// <summary>
        /// Converts a non-generic HttpResult from AspNetCoreHttpKit.
        /// </summary>
        public static ApiResponse From(HttpResult result)
        {
            if (result.IsSuccess)
                return new(true, result.StatusCode, null, null);

            var statusCode = ResponseKitDefaults.PropagateUpstreamStatusCodes
                ? result.StatusCode
                : HttpStatusCode.InternalServerError;

            return new(false, statusCode, result.ErrorMessage ?? "Upstream error.", null);
        }

        // ----------------------------------------------------------
        // IResult helper
        // ----------------------------------------------------------

        public IResult ToResult()
            => Results.Json(this, statusCode: (int)StatusCode);
    }
}
