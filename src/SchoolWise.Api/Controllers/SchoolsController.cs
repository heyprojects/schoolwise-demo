using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolWise.Application.Schools;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Api.Controllers;

[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/schools")]
public sealed class SchoolsController(ISender sender) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? keyword = null,
        [FromQuery] string? city = null,
        [FromQuery] SchoolType? type = null,
        [FromQuery] bool? isPrivate = null,
        [FromQuery] decimal? minTuition = null,
        [FromQuery] decimal? maxTuition = null,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchSchoolsQuery(page, pageSize, sortBy, sortDirection, keyword, city, type, isPrivate, minTuition, maxTuition);
        return FromResult(await sender.Send(query, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new GetSchoolByIdQuery(id), cancellationToken));

    [AllowAnonymous]
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new GetSchoolBySlugQuery(slug), cancellationToken));

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin")]
    public async Task<IActionResult> SearchForAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? keyword = null,
        [FromQuery] string? city = null,
        [FromQuery] SchoolType? type = null,
        [FromQuery] bool? isPrivate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchSchoolsQuery(page, pageSize, sortBy, sortDirection, keyword, city, type, isPrivate, IncludeUnapproved: true);
        return FromResult(await sender.Send(query, cancellationToken));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin/{id:guid}")]
    public async Task<IActionResult> GetByIdForAdmin(Guid id, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new GetSchoolByIdQuery(id, IncludeUnapproved: true), cancellationToken));

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<IActionResult> Create(SchoolWriteDto school, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSchoolCommand(school), cancellationToken);
        if (!result.IsSuccess)
        {
            return FromResult(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1" }, result.Value);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SchoolWriteDto school, CancellationToken cancellationToken) =>
        FromResult(await sender.Send(new UpdateSchoolCommand(id, school), cancellationToken));

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteSchoolCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : FromResult(result);
    }
}
