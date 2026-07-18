
using System.Collections.Concurrent;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sharkable;

internal sealed class ValidationFilter : IEndpointFilter
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IValidator?>> _validatorCache = new();

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var allErrors = new List<(string PropertyName, string ErrorMessage)>();

        foreach (var arg in context.Arguments)
        {
            if (arg is null)
                continue;

            var argType = arg.GetType();
            var validatorFactory = _validatorCache.GetOrAdd(argType, static t =>
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(t);
                return sp => sp.GetService(validatorType) as IValidator;
            });
            var validator = validatorFactory(context.HttpContext.RequestServices);

            if (validator is null)
                continue;

            var validationContext = new ValidationContext<object?>(arg);
            var result = await validator.ValidateAsync(validationContext);

            if (result.IsValid)
                continue;

            foreach (var error in result.Errors)
            {
                allErrors.Add((error.PropertyName, error.ErrorMessage));
            }
        }

        if (allErrors.Count == 0)
            return await next(context);

        var mode = Shark.SharkOption.ValidationErrorMode;

        if (mode == ValidationErrorMode.ProblemDetails)
        {
            var errorsDict = allErrors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => string.IsNullOrEmpty(g.Key) ? "general" : g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problemDetails = new ValidationProblemDetails(errorsDict)
            {
                Status = 400,
                Title = "Validation failed",
                Type = "https://tools.ietf.org/html/rfc7807",
                Detail = "One or more validation errors occurred."
            };

            context.HttpContext.Response.StatusCode = 400;
            context.HttpContext.Response.ContentType = "application/problem+json";
            await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType());
            return new ValidationShortCircuit();
        }

        var errorMessage = string.Join("; ", allErrors.Select(e => e.ErrorMessage));
        var factory = UnifiedResultFactoryHelper.ResolveFactory();
        var body = factory.Create(null, errorMessage, 400);

        context.HttpContext.Response.StatusCode = 400;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(body, body.GetType());
        return new ValidationShortCircuit();
    }

    /// <summary>
    /// A no-op <see cref="IResult"/> used to short-circuit the filter pipeline
    /// after the validation error has been written directly to the response.
    /// </summary>
    private sealed class ValidationShortCircuit : IResult
    {
        public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
    }
}
