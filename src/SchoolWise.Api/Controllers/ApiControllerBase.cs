using Microsoft.AspNetCore.Mvc;
using SchoolWise.Application.Common;

namespace SchoolWise.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var status = result.Error?.Code switch
        {
            "email_taken" or "slug_taken" => StatusCodes.Status409Conflict,
            "school_not_found" or "user_not_found" => StatusCodes.Status404NotFound,
            "invalid_credentials" or "invalid_refresh_token" => StatusCodes.Status401Unauthorized,
            "forbidden" => StatusCodes.Status403Forbidden,
            "verification_code_not_found" => StatusCodes.Status404NotFound,
            "verification_conflict" => StatusCodes.Status409Conflict,
            "review_exists" or "review_conflict" => StatusCodes.Status409Conflict,
            "review_not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
        var details = new ProblemDetails
        {
            Status = status,
            Title = result.Error?.Message,
            Type = $"https://httpstatuses.com/{status}",
            Instance = HttpContext.Request.Path
        };
        details.Extensions["code"] = result.Error?.Code;
        details.Extensions["requestId"] = HttpContext.TraceIdentifier;
        return new ObjectResult(details) { StatusCode = status };
    }
}
