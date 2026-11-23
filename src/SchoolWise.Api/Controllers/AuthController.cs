using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWise.Application.Auth;

namespace SchoolWise.Api.Controllers;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(
    ISender sender,
    IHostEnvironment environment,
    IConfiguration configuration) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromResult(result);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return FromResult(result);
        }

        SetRefreshCookie(result.Value!);
        return Ok(ToSessionResponse(result.Value!));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !HasAllowedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!Request.Cookies.TryGetValue("schoolwise_refresh", out var refreshToken))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        if (!result.IsSuccess)
        {
            Response.Cookies.Delete("schoolwise_refresh", CookieOptions());
            return FromResult(result);
        }

        SetRefreshCookie(result.Value!);
        return Ok(ToSessionResponse(result.Value!));
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !HasAllowedOrigin())
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!Guid.TryParse(User.FindFirstValue("sub"), out var userId))
        {
            return Unauthorized();
        }

        if (Request.Cookies.TryGetValue("schoolwise_refresh", out var refreshToken))
        {
            var result = await sender.Send(new RevokeRefreshTokenCommand(userId, refreshToken), cancellationToken);
            if (!result.IsSuccess)
            {
                Response.Cookies.Delete("schoolwise_refresh", CookieOptions());
                return FromResult(result);
            }
        }

        Response.Cookies.Delete("schoolwise_refresh", CookieOptions());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var userId))
        {
            return Unauthorized();
        }

        return FromResult(await sender.Send(new GetCurrentUserQuery(userId), cancellationToken));
    }

    private void SetRefreshCookie(AuthTokensDto tokens) =>
        Response.Cookies.Append("schoolwise_refresh", tokens.RefreshToken, CookieOptions(tokens.RefreshExpiresAt));

    private CookieOptions CookieOptions(DateTimeOffset? expiresAt = null) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
        Path = "/api/v1/auth",
        Expires = expiresAt
    };

    private bool HasAllowedOrigin()
    {
        var origin = Request.Headers.Origin.ToString();
        var configuredOrigins = configuration["Frontend:Origins"]
            ?? configuration["Frontend:Origin"]
            ?? "http://localhost:3000";

        return !string.IsNullOrWhiteSpace(origin)
            && configuredOrigins
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    private static AuthSessionResponse ToSessionResponse(AuthTokensDto tokens) =>
        new(tokens.AccessToken, tokens.ExpiresAt, tokens.User);

    public sealed record AuthSessionResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User);
}
