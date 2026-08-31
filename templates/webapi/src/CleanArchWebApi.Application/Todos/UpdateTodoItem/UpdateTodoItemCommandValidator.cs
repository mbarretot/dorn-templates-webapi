using FluentValidation;

namespace CleanArchWebApi.Application.Todos.UpdateTodoItem;

public sealed class UpdateTodoItemCommandValidator : AbstractValidator<UpdateTodoItemCommand>
{
    public UpdateTodoItemCommandValidator()
    {
        RuleFor(command => command.Title).NotEmpty().MaximumLength(200);
    }
}
