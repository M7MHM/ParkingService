using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Parking.Domain.Exceptions;
using ParkingBooking.Domain.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Parking_Api.Middleware
{
    public sealed class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                if (context.Response.HasStarted)
                    throw;

                var (status, title, detail) = MapException(exception);
                if (status >= 500)
                    _logger.LogError(exception, "Unhandled API error. TraceId: {TraceId}", context.TraceIdentifier);
                else
                    _logger.LogInformation("API returned {StatusCode}. TraceId: {TraceId}", status, context.TraceIdentifier);

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";

                if (exception is ValidationException validationException)
                {
                    var errors = validationException.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

                    var validationProblem = new ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "One or more validation errors occurred.",
                        Instance = context.Request.Path
                    };
                    validationProblem.Extensions["traceId"] = context.TraceIdentifier;
                    await context.Response.WriteAsJsonAsync(
                        validationProblem,
                        cancellationToken: context.RequestAborted);
                    return;
                }

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail,
                    Instance = context.Request.Path
                };
                problem.Extensions["traceId"] = context.TraceIdentifier;
                await context.Response.WriteAsJsonAsync(problem, cancellationToken: context.RequestAborted);
            }
        }

        private static (int Status, string Title, string Detail) MapException(Exception exception) => exception switch
        {
            ValidationException => (400, "Validation failed.", "One or more request fields are invalid."),
            InvalidCredentialsException => (401, "Invalid credentials.", "The email or password is incorrect."),
            EntityNotFoundException => (404, "Resource not found.", "The requested resource was not found."),
            SpotNotFoundException => (404, "Parking spot not found.", "The requested spot was not found in this parking lot."),
            EmailAlreadyRegisteredException => (409, "Email already registered.", "An account already exists for this email."),
            SpotNotAvailableException => (409, "Parking spot unavailable.", "The requested spot is not currently available."),
            InvalidBookingStatusTransitionException => (409, "Booking state conflict.", "The booking cannot move to the requested state."),
            BookingCannotBeCancelledException => (409, "Booking cannot be cancelled.", "The booking can no longer be cancelled."),
            DomainException => (400, "Business rule violated.", "The request violates a business rule."),
            ArgumentException => (400, "Invalid request.", "One or more request values are invalid."),
            InvalidOperationException => (409, "Operation conflict.", "The requested operation conflicts with the current state."),
            _ => (500, "Server error.", "An unexpected error occurred.")
        };
    }
}
