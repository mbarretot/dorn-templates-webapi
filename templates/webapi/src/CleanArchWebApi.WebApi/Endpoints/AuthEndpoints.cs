#if (UseCustomAuth)
using System.Text.Json;
using CleanArchWebApi.Application.Auth.Login;
using CleanArchWebApi.Application.Auth.Refresh;

namespace CleanArchWebApi.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/auth/login",
            async (HttpContext httpContext, ISender sender, CancellationToken ct) =>
            {
                LoginCommand? command;
                try
                {
                    command = await httpContext.Request.ReadFromJsonAsync<LoginCommand>(ct);
                }
                catch (JsonException)
                {
                    return Results.BadRequest();
                }

                if (command is null)
                {
                    return Results.BadRequest();
                }

                var result = await sender.Send(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
            }
        );

        app.MapPost(
            "/auth/refresh",
            async (HttpContext httpContext, ISender sender, CancellationToken ct) =>
            {
                RefreshTokenCommand? command;
                try
                {
                    command = await httpContext.Request.ReadFromJsonAsync<RefreshTokenCommand>(ct);
                }
                catch (JsonException)
                {
                    return Results.BadRequest();
                }

                if (command is null)
                {
                    return Results.BadRequest();
                }

                var result = await sender.Send(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
            }
        );

        return app;
    }
}
#endif
