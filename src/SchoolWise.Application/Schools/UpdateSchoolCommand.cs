using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;

namespace SchoolWise.Application.Schools;

public sealed record UpdateSchoolCommand(Guid Id, SchoolWriteDto School) : IRequest<Result<SchoolDto>>;

public sealed class UpdateSchoolCommandValidator : AbstractValidator<UpdateSchoolCommand>
{
    public UpdateSchoolCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.School).NotNull().SetValidator(new SchoolWriteValidator());
    }
}

public sealed class UpdateSchoolCommandHandler(
    ISchoolRepository schools,
    IUnitOfWork unitOfWork,
    AutoMapper.IMapper mapper) : IRequestHandler<UpdateSchoolCommand, Result<SchoolDto>>
{
    public async Task<Result<SchoolDto>> Handle(UpdateSchoolCommand request, CancellationToken cancellationToken)
    {
        var school = await schools.GetByIdAsync(request.Id, true, cancellationToken);
        if (school is null)
        {
            return Result<SchoolDto>.Failure("school_not_found", "The requested school was not found.");
        }

        var slug = CreateSchoolCommandHandler.CreateSlug(request.School.Name);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = $"school-{school.Id:N}";
        }
        if (await schools.SlugExistsAsync(slug, school.Id, cancellationToken))
        {
            return Result<SchoolDto>.Failure("slug_taken", "A school with this name already exists.");
        }

        var input = request.School;
        school.Update(input.Name, slug, input.Description, input.SchoolType, input.Address, input.City,
            input.PostalCode, input.Phone, input.Email, input.Website, input.TuitionFrom, input.TuitionTo,
            input.StudentCapacity, input.EstablishedYear, input.IsPrivate, input.LogoUrl, input.IsApproved);
        school.ReplacePhotos(input.Photos.OrderBy(photo => photo.SortOrder).Select(photo => photo.Url));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SchoolDto>.Success(mapper.Map<SchoolDto>(school));
    }
}
