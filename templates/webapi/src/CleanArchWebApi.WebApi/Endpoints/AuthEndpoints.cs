#if (UseCustomAuth)
using System.Text.Json;
using CleanArchWebApi.Application.Auth.Login;

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

        return app;
    }
}
#endif
