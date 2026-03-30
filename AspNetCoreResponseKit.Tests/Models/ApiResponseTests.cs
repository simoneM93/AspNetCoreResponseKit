using AspNetCoreResponseKit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCoreResponseKit.Tests.Models
{
    internal sealed record TestUser(int Id, string Name);

    public class ApiResponse_ExplicitFactories
    {
        [Fact]
        public void Ok_ReturnsSuccessWithData()
        {
            var user = new TestUser(1, "Simone");
            var result = ApiResponse.Ok(user);

            Assert.True(result.IsSuccess);
            Assert.Equal(user, result.Data);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Null(result.Message);
        }

        [Fact]
        public void Ok_WithMessage_SetsMessage()
        {
            var result = ApiResponse.Ok(new TestUser(1, "Simone"), "User retrieved.");

            Assert.True(result.IsSuccess);
            Assert.Equal("User retrieved.", result.Message);
        }

        [Fact]
        public void Created_Returns201()
        {
            var result = ApiResponse.Created(new TestUser(1, "Simone"));

            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }

        [Fact]
        public void NoContent_Returns204()
        {
            var result = ApiResponse.NoContent();

            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        }

        [Fact]
        public void NotFound_ReturnsFailureWith404()
        {
            var result = ApiResponse.NotFound<TestUser>("User not found.");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal("User not found.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public void BadRequest_ReturnsFailureWith400()
        {
            var result = ApiResponse.BadRequest<TestUser>("Invalid input.");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public void Unauthorized_ReturnsFailureWith401()
        {
            var result = ApiResponse.Unauthorized<TestUser>();

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public void Forbidden_ReturnsFailureWith403()
        {
            var result = ApiResponse.Forbidden<TestUser>();

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);
        }

        [Fact]
        public void Conflict_ReturnsFailureWith409()
        {
            var result = ApiResponse.Conflict<TestUser>("Already exists.");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        }

        [Fact]
        public void Error_ReturnsFailureWith500ByDefault()
        {
            var result = ApiResponse.Error("Something went wrong.");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
            Assert.Equal("Something went wrong.", result.Message);
        }

        [Fact]
        public void Error_WithCustomStatusCode_UsesProvidedCode()
        {
            var result = ApiResponse.Error<TestUser>("Unprocessable.", HttpStatusCode.UnprocessableEntity);

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        }
    }

    public class ApiResponse_From
    {
        [Fact]
        public void From_NonNullValue_ReturnsOk()
        {
            var user = new TestUser(1, "Simone");
            var result = ApiResponse.From(user);

            Assert.True(result.IsSuccess);
            Assert.Equal(user, result.Data);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Fact]
        public void From_NullValue_ReturnsConfiguredNullStatusCode()
        {
            ResponseKitDefaults.NullStatusCode = HttpStatusCode.NotFound;
            ResponseKitDefaults.NullMessage = "Resource not found.";

            var result = ApiResponse.From<TestUser>(null);

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
            Assert.Equal("Resource not found.", result.Message);
            Assert.Null(result.Data);
        }

        [Fact]
        public void From_NullValue_WithCustomMessage_UsesCustomMessage()
        {
            var result = ApiResponse.From<TestUser>(null, "User does not exist.");

            Assert.False(result.IsSuccess);
            Assert.Equal("User does not exist.", result.Message);
        }

        [Fact]
        public void From_NullValue_WithCustomStatusCode_UsesCustomCode()
        {
            var result = ApiResponse.From<TestUser>(null, nullStatusCode: HttpStatusCode.BadRequest);

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }

    public class ApiResponse_FromAsync
    {
        [Fact]
        public async Task FromAsync_NonNullValue_ReturnsOk()
        {
            var user = new TestUser(1, "Simone");
            var result = await ApiResponse.FromAsync<TestUser>(() => Task.FromResult<TestUser?>(user));

            Assert.True(result.IsSuccess);
            Assert.Equal(user, result.Data);
        }

        [Fact]
        public async Task FromAsync_NullValue_ReturnsConfiguredNullStatusCode()
        {
            ResponseKitDefaults.NullStatusCode = HttpStatusCode.NotFound;

            var result = await ApiResponse.FromAsync<TestUser>(() => Task.FromResult<TestUser?>(null));

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public async Task FromAsync_CancelledToken_ThrowsOperationCanceledException()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                ApiResponse.FromAsync<TestUser>(
                    () => Task.FromResult<TestUser?>(null),
                    cancellationToken: cts.Token));
        }

        [Fact]
        public async Task FromAsync_WithNullMessage_UsesCustomMessage()
        {
            var result = await ApiResponse.FromAsync<TestUser>(
                () => Task.FromResult<TestUser?>(null),
                nullMessage: "Custom not found.");

            Assert.Equal("Custom not found.", result.Message);
        }
    }

    public class ApiResponse_FromBool
    {
        [Fact]
        public void FromBool_True_ReturnsSuccess()
        {
            var result = ApiResponse.FromBool(true, "Deleted.", "Not found.");

            Assert.True(result.IsSuccess);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("Deleted.", result.Message);
        }

        [Fact]
        public void FromBool_False_ReturnsFailure()
        {
            var result = ApiResponse.FromBool(false, "Deleted.", "Not found.");

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal("Not found.", result.Message);
        }

        [Fact]
        public void FromBool_False_WithCustomFailureCode_UsesCustomCode()
        {
            var result = ApiResponse.FromBool(false, "ok", "fail", HttpStatusCode.NotFound);

            Assert.False(result.IsSuccess);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }
    }

    public class ApiResponse_FluentApi
    {
        [Fact]
        public void OnNull_WhenDataIsNull_OverridesResponse()
        {
            var result = ApiResponse.From<TestUser>(null)
                .OnNull("Custom not found.", HttpStatusCode.NotFound);

            Assert.False(result.IsSuccess);
            Assert.Equal("Custom not found.", result.Message);
            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        [Fact]
        public void OnNull_WhenDataIsNotNull_DoesNothing()
        {
            var user = new TestUser(1, "Simone");
            var result = ApiResponse.From(user)
                .OnNull("Should not appear.");

            Assert.True(result.IsSuccess);
            Assert.Equal(user, result.Data);
        }

        [Fact]
        public void OnSuccess_WhenSuccessful_TransformsData()
        {
            var user = new TestUser(1, "Simone");
            var result = ApiResponse.Ok(user)
                .OnSuccess(u => u?.Name.ToUpper());

            Assert.True(result.IsSuccess);
            Assert.Equal("SIMONE", result.Data);
        }

        [Fact]
        public void OnSuccess_WhenFailed_DoesNotTransform()
        {
            var result = ApiResponse.NotFound<TestUser>("Not found.")
                .OnSuccess(u => u?.Name.ToUpper());

            Assert.False(result.IsSuccess);
            Assert.Null(result.Data);
        }

        [Fact]
        public void ChainedFluentCalls_WorkCorrectly()
        {
            var user = new TestUser(1, "Simone");

            var result = ApiResponse.From(user)
                .OnNull("Not found.")
                .OnSuccess(u => u?.Id.ToString());

            Assert.True(result.IsSuccess);
            Assert.Equal("1", result.Data);
        }
    }
}
