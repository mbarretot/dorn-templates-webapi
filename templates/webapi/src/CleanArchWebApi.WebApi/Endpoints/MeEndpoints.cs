#if (UseAuth)
using System.Security.Claims;

namespace CleanArchWebApi.WebApi.Endpoints;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me").WithTags("Auth").RequireAuthorization();

        group.MapGet(
            "/",
            (ClaimsPrincipal user) =>
            {
                var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToArray();
                return Results.Ok(claims);
            }
        );

        return app;
    }
}
#endif
