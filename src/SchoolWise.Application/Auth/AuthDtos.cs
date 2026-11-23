using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Auth;

public sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record AuthTokensDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    UserDto User);
