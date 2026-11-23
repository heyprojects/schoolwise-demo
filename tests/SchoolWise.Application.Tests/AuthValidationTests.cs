using SchoolWise.Application.Auth;

namespace SchoolWise.Application.Tests;

public sealed class AuthValidationTests
{
    [Fact]
    public async Task RegisterValidatorAcceptsStrongParentRegistration()
    {
        var validator = new RegisterCommandValidator();
        var result = await validator.ValidateAsync(
            new RegisterCommand("Maria", "Papadopoulou", "maria@example.gr", "Schoolwise123"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("lowercase1234")]
    [InlineData("UPPERCASE1234")]
    [InlineData("NoDigitsHere")]
    public async Task RegisterValidatorRejectsWeakPasswords(string password)
    {
        var validator = new RegisterCommandValidator();
        var result = await validator.ValidateAsync(
            new RegisterCommand("Maria", "Papadopoulou", "maria@example.gr", password));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public async Task RegisterValidatorRejectsInvalidEmail()
    {
        var validator = new RegisterCommandValidator();
        var result = await validator.ValidateAsync(
            new RegisterCommand("Maria", "Papadopoulou", "not-an-email", "Schoolwise123"));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RegisterCommand.Email));
    }
}
