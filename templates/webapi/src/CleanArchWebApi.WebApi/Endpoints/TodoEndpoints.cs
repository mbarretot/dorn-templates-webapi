using CleanArchWebApi.Application.Todos.CreateTodoItem;
using CleanArchWebApi.Application.Todos.GetTodoItems;

namespace CleanArchWebApi.WebApi.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos").WithTags("Todos");

        group.MapPost(
            "/",
            async (CreateTodoItemCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/todos/{id}", id);
            }
        );

        group.MapGet(
            "/",
            async (ISender sender, CancellationToken ct) =>
            {
                var items = await sender.Send(new GetTodoItemsQuery(), ct);
                return Results.Ok(items);
            }
        );

        return app;
    }
}
