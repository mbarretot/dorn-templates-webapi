using CleanArchWebApi.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;

namespace CleanArchWebApi.Application.Tests.Common.Behaviors;

public sealed class ValidationBehaviorTests
{
    public sealed record TestRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenValidatorFails_ThrowsValidationException_AndDoesNotCallNext()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(
                new ValidationResult(
                    new[] { new ValidationFailure("Value", "Value must not be empty") }
                )
            );

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });
        var request = new TestRequest(string.Empty);
        var nextCalled = false;
        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("result");
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(request, next, CancellationToken.None)
        );

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ReturnsNextResult()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });
        var request = new TestRequest("valid");
        RequestHandlerDelegate<string> next = () => Task.FromResult("result");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.Equal("result", result);
    }

    [Fact]
    public async Task Handle_WhenNoValidatorsRegistered_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(
            Array.Empty<IValidator<TestRequest>>()
        );
        var request = new TestRequest("valid");
        RequestHandlerDelegate<string> next = () => Task.FromResult("result");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.Equal("result", result);
    }
}
