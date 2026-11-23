using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SchoolWise.Application.Reviews;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Api.Controllers;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/reviews")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReviewsController(ISender sender) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("/api/v{version:apiVersion}/schools/{schoolId:guid}/reviews")]
    public async Task<IActionResult> Public(Guid schoolId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? rating = null, [FromQuery] string sort = "newest", CancellationToken cancellationToken = default) =>
        FromResult(await sender.Send(new PublicSchoolReviewsQuery(schoolId, page, pageSize, rating, sort), cancellationToken));

    [AllowAnonymous]
    [HttpGet("/api/v{version:apiVersion}/schools/{schoolId:guid}/ratings")]
    public async Task<IActionResult> Ratings(Guid schoolId, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new SchoolRatingsQuery(schoolId), cancellationToken));

    [Authorize(Policy = "ParentOnly")]
    [EnableRateLimiting("review-write")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : FromResult(result);
    }

    [Authorize(Policy = "ParentOnly")]
    [EnableRateLimiting("review-write")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, EditReviewInput input, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new EditReviewCommand(id, input.Version, input.Content), cancellationToken));

    [Authorize(Policy = "ParentOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] int version, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteReviewCommand(id, version), cancellationToken);
        return result.IsSuccess ? NoContent() : FromResult(result);
    }

    [Authorize(Policy = "ParentOnly")]
    [HttpGet("mine")]
    public async Task<IActionResult> Mine([FromQuery] ReviewStatus? status = null, [FromQuery] Guid? schoolId = null,
        [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        FromResult(await sender.Send(new ListManagedReviewsQuery(false, status, schoolId, page), cancellationToken));

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("moderation")]
    public async Task<IActionResult> Moderation([FromQuery] ReviewStatus? status = ReviewStatus.PendingModeration,
        [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        FromResult(await sender.Send(new ListManagedReviewsQuery(true, status, Page: page), cancellationToken));

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{id:guid}/moderation")]
    public async Task<IActionResult> Moderate(Guid id, ModerationInput input, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new ModerateReviewCommand(id, input.Version, input.Approve, input.Notes), cancellationToken));

    [Authorize]
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> History(Guid id, [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        FromResult(await sender.Send(new ReviewHistoryQuery(id, page), cancellationToken));

    public sealed record EditReviewInput(int Version, ReviewWriteDto Content);
    public sealed record ModerationInput(int Version, bool Approve, string? Notes);
}
