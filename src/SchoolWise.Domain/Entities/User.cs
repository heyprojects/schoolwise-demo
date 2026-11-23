using SchoolWise.Domain.Enums;

namespace SchoolWise.Domain.Entities;

public sealed class User
{
    private User() { }

    public User(string firstName, string lastName, string email, string passwordHash, UserRole role = UserRole.Parent)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void Deactivate() => IsActive = false;
}
