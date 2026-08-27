#if (UseCustomAuth)
using CleanArchWebApi.Application.Auth.Login;
using FluentValidation.TestHelper;

namespace CleanArchWebApi.Application.Tests.Auth;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyEmail_Fails()
    {
        var command = new LoginCommand(string.Empty, TestCredentials.DemoPassword);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WithMalformedEmail_Fails()
    {
        var command = new LoginCommand("not-an-email", TestCredentials.DemoPassword);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Validate_WithEmptyPassword_Fails()
    {
        var command = new LoginCommand("user@example.com", string.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validate_WithShortPassword_Fails()
    {
        var command = new LoginCommand("user@example.com", "short");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Validate_WithValidCommand_Passes()
    {
        var command = new LoginCommand("user@example.com", TestCredentials.DemoPassword);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

#endif
