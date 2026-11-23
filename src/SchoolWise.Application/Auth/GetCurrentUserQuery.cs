using MediatR;
using SchoolWise.Application.Common;

namespace SchoolWise.Application.Auth;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserDto>>;

public sealed class GetCurrentUserQueryHandler(
    IUserRepository users,
    AutoMapper.IMapper mapper) : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        return user is null || !user.IsActive
            ? Result<UserDto>.Failure("user_not_found", "The current user is no longer available.")
            : Result<UserDto>.Success(mapper.Map<UserDto>(user));
    }
}
