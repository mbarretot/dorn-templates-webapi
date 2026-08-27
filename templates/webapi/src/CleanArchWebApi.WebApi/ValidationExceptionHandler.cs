using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace CleanArchWebApi.WebApi;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct
    )
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var errors = validationException
            .Errors.GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray()
            );

        var result = TypedResults.ValidationProblem(errors);

        await result.ExecuteAsync(httpContext);

        return true;
    }
}
