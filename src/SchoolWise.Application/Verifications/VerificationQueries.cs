using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Verifications;

public sealed record ListVerificationCodesQuery(Guid SchoolId, int Page = 1)
    : IRequest<Result<PagedResult<CodeStatusDto>>>;
public sealed record ListOwnVerificationsQuery(int Page = 1)
    : IRequest<Result<PagedResult<ParentVerificationDto>>>;

public sealed class ListVerificationCodesQueryValidator : AbstractValidator<ListVerificationCodesQuery>
{
    public ListVerificationCodesQueryValidator()
    {
        RuleFor(query => query.SchoolId).NotEmpty();
        RuleFor(query => query.Page).InclusiveBetween(1, 100000);
    }
}

public sealed class ListOwnVerificationsQueryValidator : AbstractValidator<ListOwnVerificationsQuery>
{
    public ListOwnVerificationsQueryValidator() => RuleFor(query => query.Page).InclusiveBetween(1, 100000);
}

public sealed class ListVerificationCodesQueryHandler(ICurrentUser currentUser, IUserRepository users,
    IVerificationRepository verifications)
    : IRequestHandler<ListVerificationCodesQuery, Result<PagedResult<CodeStatusDto>>>
{
    public async Task<Result<PagedResult<CodeStatusDto>>> Handle(ListVerificationCodesQuery request,
        CancellationToken cancellationToken)
    {
        var user = currentUser.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is not { IsActive: true, Role: UserRole.Admin })
        {
            return Result<PagedResult<CodeStatusDto>>.Failure("forbidden", "Administrator access is required.");
        }
        return Result<PagedResult<CodeStatusDto>>.Success(
            await verifications.ListCodesAsync(request.SchoolId, request.Page, cancellationToken));
    }
}

public sealed class ListOwnVerificationsQueryHandler(ICurrentUser currentUser, IUserRepository users,
    IVerificationRepository verifications)
    : IRequestHandler<ListOwnVerificationsQuery, Result<PagedResult<ParentVerificationDto>>>
{
    public async Task<Result<PagedResult<ParentVerificationDto>>> Handle(ListOwnVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        var user = currentUser.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is not { IsActive: true, Role: UserRole.Parent })
        {
            return Result<PagedResult<ParentVerificationDto>>.Failure("forbidden", "An active Parent account is required.");
        }
        return Result<PagedResult<ParentVerificationDto>>.Success(
            await verifications.ListOwnAsync(user.Id, request.Page, DateTimeOffset.UtcNow, cancellationToken));
    }
}
