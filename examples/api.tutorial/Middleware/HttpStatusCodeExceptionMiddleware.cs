using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace api.tutorial
{
    public abstract class StatusCodeException : Exception
    {
        public StatusCodeException(string message) : base(message) { }
    }

    public sealed class ConflictException : StatusCodeException
    {
        public ConflictException(string message) : base(message) { }
    }

    public sealed class NotFoundException : StatusCodeException
    {
        public NotFoundException(string message) : base(message) { }
    }

    public sealed class BadRequestException : StatusCodeException
    {
        public BadRequestException(string message) : base(message) { }
    }

    public sealed class UnauthorizedException : StatusCodeException
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    public class HttpStatusCodeExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<HttpStatusCodeExceptionMiddleware> logger;

        public HttpStatusCodeExceptionMiddleware(
            ILogger<HttpStatusCodeExceptionMiddleware> logger,
            RequestDelegate next)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;

            if (exception is NotFoundException) code = HttpStatusCode.NotFound;
            else if (exception is UnauthorizedException) code = HttpStatusCode.Unauthorized;
            else if (exception is BadRequestException) code = HttpStatusCode.BadRequest;
            else if (exception is ConflictException) code = HttpStatusCode.Conflict;

            var result = JsonConvert.SerializeObject(new { error = exception.Message });
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            return context.Response.WriteAsync(result);
        }
    }
}
