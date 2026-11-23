using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Auth;

public sealed record RegisterCommand(string FirstName, string LastName, string Email, string Password)
    : IRequest<Result<UserDto>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(command => command.Password).MinimumLength(10).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

public sealed class RegisterCommandHandler(
    IUserRepository users,
    IPasswordService passwords,
    IUnitOfWork unitOfWork,
    AutoMapper.IMapper mapper) : IRequestHandler<RegisterCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return Result<UserDto>.Failure("email_taken", "An account with this email already exists.");
        }

        var user = new User(request.FirstName, request.LastName, email, string.Empty, UserRole.Parent);
        user.SetPasswordHash(passwords.Hash(user, request.Password));
        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<UserDto>.Success(mapper.Map<UserDto>(user));
    }
}
