using CleanArchitectureDemo.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace CleanArchitectureDemo.Api.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, problemDetails) = exception switch
            {
                NotFoundException notFound => (
                    HttpStatusCode.NotFound,
                    new ProblemDetails
                    {
                        Status = (int)HttpStatusCode.NotFound,
                        Title = "Resource Not Found",
                        Detail = notFound.Message,
                        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4"
                    }
                ),
                ConflictException conflict => (
                    HttpStatusCode.Conflict,
                    new ProblemDetails
                    {
                        Status = (int)HttpStatusCode.Conflict,
                        Title = "Conflict",
                        Detail = conflict.Message,
                        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8"
                    }
                ),
                ValidationException validation => (
                    HttpStatusCode.BadRequest,
                    new ValidationProblemDetails(validation.Errors)
                    {
                        Status = (int)HttpStatusCode.BadRequest,
                        Title = "Validation Error",
                        Detail = validation.Message,
                        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
                    }
                ),
                FluentValidation.ValidationException fluentValidation => (
                    HttpStatusCode.BadRequest,
                    new ValidationProblemDetails(
                        fluentValidation.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()
                            )
                    )
                    {
                        Status = (int)HttpStatusCode.BadRequest,
                        Title = "Validation Error",
                        Detail = "One or more validation errors occurred.",
                        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
                    }
                ),
                _ => (
                    HttpStatusCode.InternalServerError,
                    new ProblemDetails
                    {
                        Status = (int)HttpStatusCode.InternalServerError,
                        Title = "Internal Server Error",
                        Detail = "An unexpected error occurred. Please try again later.",
                        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
                    }
                )
            };

            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
        }
    }
}
