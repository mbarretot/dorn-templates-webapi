using CleanArchWebApi.Application.Todos.CreateTodoItem;
using CleanArchWebApi.Application.Todos.DeleteTodoItem;
using CleanArchWebApi.Application.Todos.GetTodoItemById;
using CleanArchWebApi.Application.Todos.GetTodoItems;
using CleanArchWebApi.Application.Todos.SetTodoItemCompletion;
using CleanArchWebApi.Application.Todos.UpdateTodoItem;
#if (UseAuth)
using CleanArchWebApi.Application.Common.Security;
#endif

namespace CleanArchWebApi.WebApi.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos").WithTags("Todos");

        group
            .MapPost(
                "/",
                async (CreateTodoItemCommand command, ISender sender, CancellationToken ct) =>
                {
                    var id = await sender.Send(command, ct);
                    return Results.Created($"/api/todos/{id}", id);
                }
            )
#if (UseAuth)
            .RequireAuthorization(Permissions.TodosWrite)
#endif
        ;

        group
            .MapGet(
                "/",
                async (ISender sender, CancellationToken ct) =>
                {
                    var items = await sender.Send(new GetTodoItemsQuery(), ct);
                    return Results.Ok(items);
                }
            )
#if (UseAuth)
            .RequireAuthorization(Permissions.TodosRead)
#endif
        ;

        group
            .MapGet(
                "/{id:guid}",
                async (Guid id, ISender sender, CancellationToken ct) =>
                {
                    var item = await sender.Send(new GetTodoItemByIdQuery(id), ct);
                    return item is null ? Results.NotFound() : Results.Ok(item);
                }
            )
#if (UseAuth)
            .RequireAuthorization(Permissions.TodosRead)
#endif
        ;

        group
            .MapPut(
                "/{id:guid}",
                async (
                    Guid id,
                    UpdateTodoItemRequest request,
                    ISender sender,
                    CancellationToken ct
                ) =>
                {
                    var updated = await sender.Send(
                        new UpdateTodoItemCommand(id, request.Title),
                        ct
                    );
                    return updated ? Results.NoContent() : Results.NotFound();
                }
            )
#if (UseAuth)
            .RequireAuthorization(Permissions.TodosWrite)
#endif
        ;

        group
            .MapPatch(
                "/{id:guid}/complete",
                async (
                    Guid id,
                    SetTodoItemCompletionRequest request,
                    ISender sender,
                    CancellationToken ct
                ) =>
                {
                    var updated = await sender.Send(
                        new SetTodoItemCompletionCommand(id, request.IsComplete),
                        ct
                    );
                    return updated ? Results.NoContent() : Results.NotFound();
                }
            )
#if (UseAuth)
            .RequireAuthorization(Permissions.TodosWrite)
#endif
        ;

        group
            .MapDelete(
                "/{id:guid}",
                async (Guid id, ISender sender, CancellationToken ct) =>
                {
                    var deleted = await sender.Send(new DeleteTodoItemCommand(id), ct);
                    return deleted ? Results.NoContent() : Results.NotFound();
                }
            )
#if (UseAuth)
            .RequireAuthorization(Permissions.TodosDelete)
#endif
        ;

        return app;
    }
}

public sealed record UpdateTodoItemRequest(string Title);

public sealed record SetTodoItemCompletionRequest(bool IsComplete);
