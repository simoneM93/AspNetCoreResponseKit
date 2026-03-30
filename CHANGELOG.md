# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [1.0.0] - YYYY-MM-DD

### Added
- `ApiResponse<T>` — standardized response envelope with `IsSuccess`, `Data`, `Message`, `Errors`, `StatusCode`
- `ApiResponse` — non-generic version for operations without a response body (DELETE, etc.)
- Explicit factory methods: `Ok`, `Created`, `NoContent`, `NotFound`, `BadRequest`, `Unauthorized`, `Forbidden`, `Conflict`, `Error`
- `ApiResponse.From<T>(value)` — smart factory: returns 200 if value is not null, configured null status code otherwise
- `ApiResponse.FromAsync<T>(factory)` — async smart factory with `CancellationToken` support
- `ApiResponse.FromBool(bool, onTrue, onFalse)` — bool-based response for operations without return value
- `ApiResponse.From(HttpResult<T>)` — native conversion from `AspNetCoreHttpKit.HttpResult<T>`
- `ApiResponse.From(HttpResult)` — native conversion from non-generic `AspNetCoreHttpKit.HttpResult`
- Fluent `.OnNull(message, statusCode?)` — override response when data is null
- Fluent `.OnSuccess(transform)` — transform data on success
- `.ToResult()` — converts `ApiResponse<T>` to `IResult` for Minimal API use
- `ApiExceptionMiddleware` — global exception handler, registered via `app.UseApiExceptionHandler()`
- `IExceptionMapper` — interface for custom exception mapping implementations
- `DefaultExceptionMapper` — maps standard .NET exceptions: `ArgumentException` → 400, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404, `InvalidOperationException` → 409, `NotImplementedException` → 501, `TimeoutException` → 408, `OperationCanceledException` → 499
- `AuthKitExceptionMapper` — maps `AspNetCoreAuthKit` and `AspNetCoreHttpKit` exceptions to correct status codes
- `ExceptionMapperResolver` — resolves the correct mapper following the priority chain: custom → AuthKit → default → fallback 500
- `ResponseKitOptions` — configurable `NullResponseStatusCode`, `NullResponseMessage`, `PropagateUpstreamStatusCodes`, `IncludeExceptionDetails`, `DefaultErrorMessage`
- `ResponseKitOptions.MapException<T>()` — fluent custom exception registration
- `AddAspNetCoreResponseKit(Action<ResponseKitOptions>?)` — DI registration extension method
- `UseApiExceptionHandler()` — middleware registration extension method
- MIT license

---

[Unreleased]: https://github.com/simoneM93/AspNetCoreResponseKit/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/simoneM93/AspNetCoreResponseKit/releases/tag/v1.0.0