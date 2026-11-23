using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Tests;

public sealed class DomainEntityTests
{
    [Fact]
    public void UserNormalizesEmailAndDefaultsToParentRole()
    {
        var user = new User("Maria", "Papadopoulou", " MARIA@EXAMPLE.GR ", "hash");

        Assert.Equal("maria@example.gr", user.Email);
        Assert.Equal(UserRole.Parent, user.Role);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void RefreshTokenIsSingleUseAfterRevocation()
    {
        var token = new RefreshToken(Guid.NewGuid(), new string('a', 64), DateTimeOffset.UtcNow.AddDays(1));
        token.Revoke();

        Assert.False(token.IsActive(DateTimeOffset.UtcNow));
    }
}
