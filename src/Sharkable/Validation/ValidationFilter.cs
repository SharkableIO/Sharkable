
using System.Collections.Concurrent;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal sealed class ValidationFilter : IEndpointFilter
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IValidator?>> _validatorCache = new();

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
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

            var errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            var factory = UnifiedResultFactoryHelper.ResolveFactory();
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
