#if (IncludeLocalization)
using System.Text.Json;
using CleanArchWebApi.WebApi.Localization;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace CleanArchWebApi.Functional.Tests;

/// <summary>Culture negotiation and localized messages, over the real pipeline and the generated resources.</summary>
public sealed class LocalizationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public LocalizationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private RequestLocalizationOptions Options =>
        _factory.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;

    private static async Task<string?> ContentLanguageAsync(
        HttpClient client,
        string? acceptLanguage
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/localization-probe");
        if (acceptLanguage is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        using var response = await client.SendAsync(request);
        return response.Content.Headers.ContentLanguage.SingleOrDefault();
    }

    [Fact]
    public async Task Request_WithoutAcceptLanguage_UsesTheDefaultCulture()
    {
        var language = await ContentLanguageAsync(_factory.CreateClient(), null);

        Assert.Equal(Options.DefaultRequestCulture.UICulture.Name, language);
    }

    [Fact]
    public async Task Request_UsesEachSupportedCultureFromAcceptLanguage()
    {
        var client = _factory.CreateClient();

        foreach (var culture in Options.SupportedUICultures!)
        {
            Assert.Equal(culture.Name, await ContentLanguageAsync(client, culture.Name));
        }
    }

    [Fact]
    public async Task Request_WithAnUnsupportedCulture_FallsBackToTheDefaultCulture()
    {
        var supported = Options.SupportedUICultures!.Select(culture => culture.Name).ToHashSet();
        var unsupported = new[] { "zu-ZA", "af-ZA", "sw-KE" }.First(code =>
            !supported.Contains(code)
        );

        var language = await ContentLanguageAsync(_factory.CreateClient(), unsupported);

        Assert.Equal(Options.DefaultRequestCulture.UICulture.Name, language);
    }

    [Fact]
    public void SharedResource_ResolvesTheNeutralMessages()
    {
        var localizer = _factory.Services.GetRequiredService<IStringLocalizer<SharedResource>>();

        Assert.False(localizer["ValidationProblem.Title"].ResourceNotFound);
        Assert.False(localizer["ProblemDetails.Title.500"].ResourceNotFound);
    }

    [Fact]
    public async Task ProblemDetails_TakeTheirTitleFromTheResources()
    {
        var service = _factory.Services.GetRequiredService<IProblemDetailsService>();
        var localizer = _factory.Services.GetRequiredService<IStringLocalizer<SharedResource>>();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var written = await service.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = { Status = StatusCodes.Status500InternalServerError },
            }
        );

        Assert.True(written);
        context.Response.Body.Position = 0;
        using var body = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(
            localizer["ProblemDetails.Title.500"].Value,
            body.RootElement.GetProperty("title").GetString()
        );
    }
#if (IncludeSample)

    /// <summary>Needs two supported cultures whose FluentValidation translations differ; with fewer there is nothing to tell apart.</summary>
    [Fact]
    public async Task ValidationFailure_FollowsTheAcceptLanguageCulture()
    {
        var distinctCultures = Options
            .SupportedUICultures!.GroupBy(culture =>
                ValidatorOptions.Global.LanguageManager.GetString("NotEmptyValidator", culture)
            )
            .Select(group => group.First())
            .Take(2)
            .ToList();
        if (distinctCultures.Count < 2)
        {
            return;
        }
        var client = _factory.CreateClient();

        var first = await PostEmptyTitleAsync(client, distinctCultures[0].Name);
        var second = await PostEmptyTitleAsync(client, distinctCultures[1].Name);

        Assert.NotEqual(first, second);
    }

    private static async Task<string> PostEmptyTitleAsync(HttpClient client, string acceptLanguage)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/todos")
        {
            Content = JsonContent.Create(new { Title = "" }),
        };
        request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("errors").GetProperty("Title")[0].GetString()!;
    }
#endif
}
#endif
