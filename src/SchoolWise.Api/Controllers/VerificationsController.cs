using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SchoolWise.Application.Verifications;

namespace SchoolWise.Api.Controllers;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/verifications")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class VerificationsController(ISender sender) : ApiControllerBase
{
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("verification-issue")]
    [HttpPost("codes")]
    public async Task<IActionResult> Issue(IssueCodesDto input, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new IssueVerificationCodesCommand(input), cancellationToken));

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("codes")]
    public async Task<IActionResult> Codes([FromQuery] Guid schoolId, [FromQuery] int page = 1,
        CancellationToken cancellationToken = default) =>
        FromResult(await sender.Send(new ListVerificationCodesQuery(schoolId, page), cancellationToken));

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("codes/{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, RevokeCodeInput input, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new RevokeVerificationCodeCommand(id, input.Reason), cancellationToken));

    [Authorize(Policy = "ParentOnly")]
    [EnableRateLimiting("verification-redeem")]
    [HttpPost("redeem")]
    public async Task<IActionResult> Redeem(RedeemVerificationCodeCommand command, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(command, cancellationToken));

    [Authorize(Policy = "ParentOnly")]
    [HttpGet("me")]
    public async Task<IActionResult> Own([FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        FromResult(await sender.Send(new ListOwnVerificationsQuery(page), cancellationToken));

    public sealed record RevokeCodeInput(string Reason);
}
