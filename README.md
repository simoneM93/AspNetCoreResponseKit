# AspNetCoreResponseKit

[![NuGet](https://img.shields.io/nuget/v/AspNetCoreResponseKit.svg)](https://www.nuget.org/packages/AspNetCoreResponseKit)
[![Publish to NuGet](https://github.com/simoneM93/AspNetCoreResponseKit/actions/workflows/publish.yml/badge.svg)](https://github.com/simoneM93/AspNetCoreResponseKit/actions/workflows/publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![GitHub Sponsors](https://img.shields.io/badge/Sponsor-%E2%9D%A4-ea4aaa?logo=github-sponsors)](https://github.com/sponsors/simoneM93)
[![Changelog](https://img.shields.io/badge/Changelog-view-blue)](CHANGELOG.md)
 
Standardized API response envelope and global exception handling middleware for ASP.NET Core. Smart factory methods, automatic exception-to-status-code mapping, and native integration with [AspNetCoreAuthKit](https://github.com/simoneM93/AspNetCoreAuthKit) and [AspNetCoreHttpKit](https://github.com/simoneM93/AspNetCoreHttpKit).
 
> **Why AspNetCoreResponseKit?**
> Every project writes the same `ApiResponse` class and the same `ExceptionHandlerMiddleware`.
> AspNetCoreResponseKit gives you a production-ready implementation with zero boilerplate,
> consistent error formats, and smart factories that eliminate try/catch blocks in controllers.
 
---
 
## ✨ Features
 
- 📦 **`ApiResponse<T>`** — consistent response envelope for all endpoints
- 🧠 **Smart factories** — `ApiResponse.From(value)` and `ApiResponse.FromAsync(() => ...)` handle null and errors automatically
- 🌐 **Middleware** — `app.UseApiExceptionHandler()` catches all unhandled exceptions globally
- 🗺️ **Automatic mapping** — exceptions are mapped to HTTP status codes via a priority chain
- 🔗 **AspNetCoreAuthKit integration** — `HttpServiceException`, `RefreshTokenException` mapped automatically
- 🔗 **AspNetCoreHttpKit integration** — `ApiResponse.From(HttpResult<T>)` converts upstream results directly
- ⚙️ **Configurable** — null status code, upstream propagation, exception details, custom mappings
 
---
 
## 📋 Requirements
 
| Requirement | Minimum version |
|---|---|
| .NET | 8.0+ |
| ASP.NET Core | 8.0+ |
 
---
 
## 🚀 Installation
 
```bash
dotnet add package AspNetCoreResponseKit
```
 
---
 
## 🎯 Quick Start
 
### 1. Register services
 
```csharp
// Program.cs
builder.Services.AddAspNetCoreResponseKit(opt =>
{
    opt.IncludeExceptionDetails    = builder.Environment.IsDevelopment();
    opt.NullResponseStatusCode     = HttpStatusCode.NotFound;
    opt.PropagateUpstreamStatusCodes = true;
    opt.DefaultErrorMessage        = "An unexpected error occurred.";
 
    // Custom exception mapping
    opt.MapException<MyDomainException>(ex => (HttpStatusCode.UnprocessableEntity, ex.Message));
});
 
// Must be first in the middleware pipeline
app.UseApiExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
```
 
### 2. Use in controllers
 
```csharp
[HttpGet("{id}")]
public async Task<ApiResponse<UserDto>> GetUser(int id, CancellationToken ct)
{
    // Smart factory — returns 200 if found, configured null status code if not
    return await ApiResponse.FromAsync(() => _service.GetUserAsync(id), ct: ct);
}
 
[HttpPost]
public async Task<ApiResponse<UserDto>> CreateUser(CreateUserRequest req, CancellationToken ct)
{
    var user = await _service.CreateAsync(req, ct);
    return ApiResponse.Created(user.ToDto(), "User created successfully.");
}
 
[HttpDelete("{id}")]
public async Task<ApiResponse> DeleteUser(int id, CancellationToken ct)
{
    var deleted = await _service.DeleteAsync(id, ct);
    return ApiResponse.FromBool(deleted, "User deleted.", "User not found.");
}
```
 
### 3. Use in Minimal APIs
 
```csharp
app.MapGet("/users/{id}", async (int id, IUserService svc, CancellationToken ct) =>
{
    var result = await ApiResponse.FromAsync(() => svc.GetUserAsync(id), ct: ct);
    return result.ToResult();
});
```
 
### 4. Use with AspNetCoreHttpKit
 
```csharp
app.MapGet("/proxy/users/{id}", async (int id, IHttpService http, CancellationToken ct) =>
{
    var result = await http.GetAsync<UserDto>($"/users/{id}", ct);
 
    // Converts HttpResult<T> directly — status code propagated based on options
    return ApiResponse.FromHttpResult(result).ToResult();
});
```
 
---
 
## 📦 Response format
 
### Success
 
```json
{
  "isSuccess": true,
  "data": { "id": 1, "name": "Simone" },
  "message": null,
  "errors": [],
  "statusCode": 200
}
```
 
### Error
 
```json
{
  "isSuccess": false,
  "data": null,
  "message": "User not found.",
  "errors": [],
  "statusCode": 404
}
```
 
---
 
## 🗺️ Exception mapping priority chain
 
When an unhandled exception is caught by the middleware, it is resolved in this order:
 
1. **Custom mappings** — registered via `opt.MapException<T>()`
2. **AspNetCoreAuthKit / AspNetCoreHttpKit** — `HttpServiceException`, `RefreshTokenException`, etc.
3. **Standard .NET exceptions** — `ArgumentException`, `UnauthorizedAccessException`, etc.
4. **Fallback** — 500 Internal Server Error
 
### Built-in mappings
 
| Exception | Status code |
|---|---|
| `HttpNotFoundException` | 404 |
| `HttpUnauthorizedException` | 401 |
| `HttpForbiddenException` | 403 |
| `HttpBadRequestException` | 400 |
| `HttpConflictException` | 409 |
| `HttpUnprocessableEntityException` | 422 |
| `HttpTooManyRequestsException` | 429 |
| `HttpServerErrorException` | 500+ |
| `RefreshTokenException` | 401 |
| `ArgumentNullException` | 400 |
| `ArgumentException` | 400 |
| `UnauthorizedAccessException` | 401 |
| `KeyNotFoundException` | 404 |
| `InvalidOperationException` | 409 |
| `NotImplementedException` | 501 |
| `TimeoutException` | 408 |
| `OperationCanceledException` | 499 |
 
---
 
## 📚 API Reference
 
### `ApiResponse` factory methods
 
| Method | Description |
|---|---|
| `ApiResponse.Ok(data)` | 200 with data |
| `ApiResponse.Created(data)` | 201 with data |
| `ApiResponse.NoContent()` | 204 |
| `ApiResponse.NotFound<T>(message)` | 404 |
| `ApiResponse.BadRequest<T>(message)` | 400 |
| `ApiResponse.Unauthorized<T>(message)` | 401 |
| `ApiResponse.Forbidden<T>(message)` | 403 |
| `ApiResponse.Conflict<T>(message)` | 409 |
| `ApiResponse.Error(message, statusCode)` | Custom error |
| `ApiResponse.From(value)` | Smart — null → configured status code |
| `ApiResponse.FromAsync(factory)` | Smart async — null → configured status code |
| `ApiResponse.FromBool(bool, onTrue, onFalse)` | Bool-based result |
| `ApiResponse.From(HttpResult<T>)` | From AspNetCoreHttpKit result |
 
### `ApiResponse<T>` fluent methods
 
| Method | Description |
|---|---|
| `.OnNull(message, statusCode?)` | Override response when data is null |
| `.OnSuccess(transform)` | Transform data on success |
| `.ToResult()` | Convert to `IResult` for Minimal APIs |
 
---
 
## ⚙️ Configuration options
 
| Option | Type | Default | Description |
|---|---|---|---|
| `NullResponseStatusCode` | `HttpStatusCode` | `404` | Status code when smart factory receives null |
| `NullResponseMessage` | `string` | `"Resource not found."` | Message when smart factory receives null |
| `PropagateUpstreamStatusCodes` | `bool` | `true` | Propagate status codes from upstream HTTP calls |
| `IncludeExceptionDetails` | `bool` | `false` | Include exception message and stack trace in errors |
| `DefaultErrorMessage` | `string` | `"An unexpected error occurred."` | Fallback error message |
 
---
 
## ❤️ Support
 
If you find AspNetCoreResponseKit useful, consider sponsoring its development.
 
[![Sponsor simoneM93](https://img.shields.io/badge/Sponsor-%E2%9D%A4-ea4aaa?logo=github-sponsors&style=for-the-badge)](https://github.com/sponsors/simoneM93)
 
---
 
## 📄 License
 
MIT — see [LICENSE](LICENSE) for details.