#if (UseAuth)
using System.Text;
#if (UseCustomAuth)
using CleanArchWebApi.Infrastructure.Auth;
using Microsoft.IdentityModel.Tokens;
#endif
#if (UseAzureAdAuth)
using Microsoft.Identity.Web;
#endif
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CleanArchWebApi.WebApi.Extensions;

public static class AuthenticationExtensions
{
#if (UseCustomAuth)
    public static IServiceCollection AddCustomJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var jwt =
            configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = jwt.SigningKey ?? string.Empty;
        var isPlaceholder =
            string.IsNullOrWhiteSpace(signingKey)
            || signingKey.StartsWith("REPLACE_ME", StringComparison.Ordinal);

        if (isPlaceholder && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing or a placeholder. Configure a real signing key (env var Jwt__SigningKey or user-secrets) before deploying."
            );
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();

        return services;
    }
#endif

#if (UseAzureAdAuth)
    public static IServiceCollection AddAzureAdAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        services.AddAuthorization();

        return services;
    }
#endif
}
#endif
