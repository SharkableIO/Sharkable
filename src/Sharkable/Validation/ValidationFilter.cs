
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

/// <summary>
/// Endpoint filter that validates request parameters against registered <see cref="IValidator{T}"/> instances.
/// When validation fails, short-circuits the pipeline with a 400 <see cref="UnifiedResult{T}"/> response.
/// </summary>
internal sealed class ValidationFilter : IEndpointFilter
{
    /// <summary>
    /// Validates each non-null argument that has a registered <see cref="IValidator{T}"/> in DI.
    /// Returns the first validation error as a 400 response.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
        {
            if (arg is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator is null)
                continue;

            var validationContext = new ValidationContext<object?>(arg);
            var result = await validator.ValidateAsync(validationContext);

            if (result.IsValid)
                continue;

            var errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
            var body = factory.Create(null, errorMessage, 400);

            context.HttpContext.Response.StatusCode = 400;
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(body, body.GetType());
            return new ValidationShortCircuit();
        }

        return await next(context);
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
