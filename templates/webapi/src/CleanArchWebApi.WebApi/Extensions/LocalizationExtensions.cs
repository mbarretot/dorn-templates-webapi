#if (IncludeLocalization)
using System.Globalization;
using CleanArchWebApi.WebApi.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace CleanArchWebApi.WebApi.Extensions;

/// <summary>Culture negotiation from the Accept-Language header and localized problem titles, configured by the "Localization" section.</summary>
public static class LocalizationExtensions
{
    public const string SectionName = "Localization";

    public static IServiceCollection AddApiLocalization(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var section = configuration.GetSection(SectionName);
        var defaultCulture = section["DefaultCulture"] is { Length: > 0 } configured
            ? configured
            : "en";
        var cultures = (section.GetSection("SupportedCultures").Get<string[]>() ?? [])
            .Prepend(defaultCulture)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(GetCulture)
            .ToArray();

        services.AddLocalization();
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(defaultCulture);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
            options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
            options.ApplyCurrentCultureToResponseHeaders = true;
        });

        services
            .AddOptions<ProblemDetailsOptions>()
            .Configure<IStringLocalizer<SharedResource>>(
                (options, localizer) =>
                    options.CustomizeProblemDetails = context =>
                    {
                        var title = localizer[
                            $"ProblemDetails.Title.{context.ProblemDetails.Status}"
                        ];
                        if (!title.ResourceNotFound)
                        {
                            context.ProblemDetails.Title = title.Value;
                        }
                    }
            );

        return services;
    }

    public static IApplicationBuilder UseApiLocalization(this IApplicationBuilder app) =>
        app.UseRequestLocalization();

    private static CultureInfo GetCulture(string code)
    {
        try
        {
            return CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"'{code}' in the '{SectionName}' section is not a culture this runtime knows. Use a culture code such as en or pt-BR.",
                exception
            );
        }
    }
}
#endif
