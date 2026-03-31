using AspNetCoreAuthKit.Tokens.Exceptions;
using AspNetCoreHttpKit.Models;
using AspNetCoreResponseKit.Mapping;
using AspNetCoreResponseKit.Options;
using System.Net;
using Xunit;

namespace AspNetCoreResponseKit.Tests.Mapping
{
    public class DefaultExceptionMapper_Tests
    {
        private readonly DefaultExceptionMapper _mapper = new();

        [Fact]
        public void CanHandle_AlwaysReturnsTrue()
        {
            Assert.True(_mapper.CanHandle(new Exception("any")));
            Assert.True(_mapper.CanHandle(new ArgumentNullException()));
        }

        [Theory]
        [InlineData(typeof(ArgumentNullException), HttpStatusCode.BadRequest)]
        [InlineData(typeof(ArgumentException), HttpStatusCode.BadRequest)]
        [InlineData(typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized)]
        [InlineData(typeof(NotImplementedException), HttpStatusCode.NotImplemented)]
        [InlineData(typeof(TimeoutException), HttpStatusCode.RequestTimeout)]
        [InlineData(typeof(KeyNotFoundException), HttpStatusCode.NotFound)]
        [InlineData(typeof(InvalidOperationException), HttpStatusCode.Conflict)]
        public void Map_ReturnsCorrectStatusCode(Type exceptionType, HttpStatusCode expectedCode)
        {
            var ex = (Exception)Activator.CreateInstance(exceptionType, "test message")!;
            var (statusCode, _) = _mapper.Map(ex);
            Assert.Equal(expectedCode, statusCode);
        }

        [Fact]
        public void Map_OperationCanceledException_Returns499()
        {
            var (statusCode, _) = _mapper.Map(new OperationCanceledException());
            Assert.Equal((HttpStatusCode)499, statusCode);
        }

        [Fact]
        public void Map_UnknownException_Returns500()
        {
            var (statusCode, _) = _mapper.Map(new Exception("unknown"));
            Assert.Equal(HttpStatusCode.InternalServerError, statusCode);
        }
    }

    public class AuthKitExceptionMapper_Tests
    {
        private readonly AuthKitExceptionMapper _mapper = new();

        [Fact]
        public void CanHandle_HttpServiceException_ReturnsTrue()
            => Assert.True(_mapper.CanHandle(new HttpNotFoundException("not found")));

        [Fact]
        public void CanHandle_RefreshTokenException_ReturnsTrue()
            => Assert.True(_mapper.CanHandle(new RefreshTokenException("invalid")));

        [Fact]
        public void CanHandle_GenericException_ReturnsFalse()
            => Assert.False(_mapper.CanHandle(new Exception("generic")));

        [Theory]
        [InlineData(typeof(HttpBadRequestException), HttpStatusCode.BadRequest)]
        [InlineData(typeof(HttpUnauthorizedException), HttpStatusCode.Unauthorized)]
        [InlineData(typeof(HttpForbiddenException), HttpStatusCode.Forbidden)]
        [InlineData(typeof(HttpNotFoundException), HttpStatusCode.NotFound)]
        [InlineData(typeof(HttpConflictException), HttpStatusCode.Conflict)]
        [InlineData(typeof(HttpUnprocessableEntityException), HttpStatusCode.UnprocessableEntity)]
        [InlineData(typeof(HttpTooManyRequestsException), HttpStatusCode.TooManyRequests)]
        public void Map_HttpKitExceptions_ReturnCorrectStatusCodes(
            Type exceptionType, HttpStatusCode expectedCode)
        {
            var ex = (Exception)Activator.CreateInstance(exceptionType, "test message", null)!;
            var (statusCode, _) = _mapper.Map(ex);
            Assert.Equal(expectedCode, statusCode);
        }

        [Fact]
        public void Map_RefreshTokenException_Returns401()
        {
            var (statusCode, message) = _mapper.Map(new RefreshTokenException("Token scaduto."));
            Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
            Assert.Equal("Token scaduto.", message);
        }
    }

    public class ExceptionMapperResolver_Tests
    {
        [Fact]
        public void Resolve_CustomMapping_TakesPriorityOverAll()
        {
            var options = new ResponseKitOptions();
            options.MapException<InvalidOperationException>(
                ex => (HttpStatusCode.UnprocessableEntity, "Custom mapping."));

            var resolver = new ExceptionMapperResolver(options);
            var (statusCode, message) = resolver.Resolve(new InvalidOperationException("test"));

            Assert.Equal(HttpStatusCode.UnprocessableEntity, statusCode);
            Assert.Equal("Custom mapping.", message);
        }

        [Fact]
        public void Resolve_AuthKitException_TakesPriorityOverDefault()
        {
            var resolver = new ExceptionMapperResolver(new ResponseKitOptions());
            var (statusCode, _) = resolver.Resolve(new HttpNotFoundException("Not found."));

            Assert.Equal(HttpStatusCode.NotFound, statusCode);
        }

        [Fact]
        public void Resolve_RefreshTokenException_Returns401()
        {
            var resolver = new ExceptionMapperResolver(new ResponseKitOptions());
            var (statusCode, _) = resolver.Resolve(new RefreshTokenException("Invalid."));

            Assert.Equal(HttpStatusCode.Unauthorized, statusCode);
        }

        [Fact]
        public void Resolve_StandardException_UsesDefaultMapper()
        {
            var resolver = new ExceptionMapperResolver(new ResponseKitOptions());
            var (statusCode, _) = resolver.Resolve(new ArgumentNullException("param"));

            Assert.Equal(HttpStatusCode.BadRequest, statusCode);
        }

        [Fact]
        public void Resolve_UnknownException_Returns500()
        {
            var resolver = new ExceptionMapperResolver(new ResponseKitOptions());
            var (statusCode, _) = resolver.Resolve(new Exception("unknown"));

            Assert.Equal(HttpStatusCode.InternalServerError, statusCode);
        }

        [Fact]
        public void Resolve_CustomMapping_MatchesBaseType()
        {
            var options = new ResponseKitOptions();
            options.MapException<HttpServiceException>(
                ex => (HttpStatusCode.BadGateway, "Gateway error."));

            var resolver = new ExceptionMapperResolver(options);

            var (statusCode, message) = resolver.Resolve(new HttpNotFoundException("not found"));

            Assert.Equal(HttpStatusCode.BadGateway, statusCode);
            Assert.Equal("Gateway error.", message);
        }
    }
}
