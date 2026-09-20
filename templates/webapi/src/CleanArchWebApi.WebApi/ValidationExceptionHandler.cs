using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
#if (IncludeLocalization)
using CleanArchWebApi.WebApi.Localization;
using Microsoft.Extensions.Localization;
#endif

namespace CleanArchWebApi.WebApi;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
#if (IncludeLocalization)
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ValidationExceptionHandler(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }
#endif
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

#if (IncludeLocalization)
        var result = TypedResults.ValidationProblem(
            errors,
            title: _localizer["ValidationProblem.Title"]
        );
#else
        var result = TypedResults.ValidationProblem(errors);
#endif

        await result.ExecuteAsync(httpContext);

        return true;
    }
}
