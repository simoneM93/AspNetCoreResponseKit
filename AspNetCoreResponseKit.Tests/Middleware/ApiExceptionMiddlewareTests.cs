using AspNetCoreAuthKit.Tokens.Exceptions;
using AspNetCoreHttpKit.Models;
using AspNetCoreResponseKit.Extensions;
using AspNetCoreResponseKit.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text.Json;

namespace AspNetCoreResponseKit.Tests.Middleware
{
    internal static class MiddlewareTestFactory
    {
        public static HttpClient CreateClient(
            Exception exceptionToThrow,
            Action<ResponseKitOptions>? configure = null)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddAspNetCoreResponseKit(configure ?? (_ => { }));
                        services.AddRouting();
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseApiExceptionHandler();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/test", _ => throw exceptionToThrow);
                        });
                    });
                })
                .Build();

            host.Start();
            return host.GetTestServer().CreateClient();
        }

        public static async Task<JsonDocument> GetResponseJson(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content);
        }
    }

    public class ApiExceptionMiddleware_Tests
    {
        [Fact]
        public async Task Middleware_ArgumentException_Returns400()
        {
            var client = MiddlewareTestFactory.CreateClient(new ArgumentException("Bad input."));

            var response = await client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Middleware_UnauthorizedAccessException_Returns401()
        {
            var client = MiddlewareTestFactory.CreateClient(new UnauthorizedAccessException());

            var response = await client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Middleware_HttpNotFoundException_Returns404()
        {
            var client = MiddlewareTestFactory.CreateClient(new HttpNotFoundException("Not found."));

            var response = await client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Middleware_RefreshTokenException_Returns401()
        {
            var client = MiddlewareTestFactory.CreateClient(new RefreshTokenException("Token invalid."));

            var response = await client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Middleware_UnhandledException_Returns500()
        {
            var client = MiddlewareTestFactory.CreateClient(new Exception("Unexpected."));

            var response = await client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task Middleware_ResponseIsJson_WithIsSuccessFalse()
        {
            var client = MiddlewareTestFactory.CreateClient(new ArgumentException("Bad."));

            var response = await client.GetAsync("/test");
            var json = await MiddlewareTestFactory.GetResponseJson(response);

            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.False(json.RootElement.GetProperty("isSuccess").GetBoolean());
        }

        [Fact]
        public async Task Middleware_WithIncludeExceptionDetails_IncludesErrorDetails()
        {
            var client = MiddlewareTestFactory.CreateClient(
                new ArgumentException("Detailed error."),
                opt => opt.IncludeExceptionDetails = true);

            var response = await client.GetAsync("/test");
            var json = await MiddlewareTestFactory.GetResponseJson(response);

            var errors = json.RootElement.GetProperty("errors");
            Assert.NotEqual(0, errors.GetArrayLength());
        }

        [Fact]
        public async Task Middleware_WithoutIncludeExceptionDetails_HidesErrorDetails()
        {
            var client = MiddlewareTestFactory.CreateClient(
                new ArgumentException("Secret error."),
                opt =>
                {
                    opt.IncludeExceptionDetails = false;
                    opt.DefaultErrorMessage = "An error occurred.";
                });

            var response = await client.GetAsync("/test");
            var json = await MiddlewareTestFactory.GetResponseJson(response);

            var message = json.RootElement.GetProperty("message").GetString();
            Assert.DoesNotContain("Secret error.", message ?? string.Empty);
        }

        [Fact]
        public async Task Middleware_CustomExceptionMapping_UsesCustomStatusCode()
        {
            var client = MiddlewareTestFactory.CreateClient(
                new InvalidOperationException("Custom."),
                opt => opt.MapException<InvalidOperationException>(
                    ex => (System.Net.HttpStatusCode.UnprocessableEntity, ex.Message)));

            var response = await client.GetAsync("/test");

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }
    }
}
