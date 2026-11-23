using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;

namespace SchoolWise.Application.Schools;

public sealed record DeleteSchoolCommand(Guid Id) : IRequest<Result<bool>>;

public sealed class DeleteSchoolCommandValidator : AbstractValidator<DeleteSchoolCommand>
{
    public DeleteSchoolCommandValidator() => RuleFor(command => command.Id).NotEmpty();
}

public sealed class DeleteSchoolCommandHandler(
    ISchoolRepository schools,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteSchoolCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteSchoolCommand request, CancellationToken cancellationToken)
    {
        var school = await schools.GetByIdAsync(request.Id, true, cancellationToken);
        if (school is null)
        {
            return Result<bool>.Failure("school_not_found", "The requested school was not found.");
        }

        schools.Remove(school);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
