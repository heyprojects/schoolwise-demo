using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;

namespace SchoolWise.Application.Schools;

public sealed record CreateSchoolCommand(SchoolWriteDto School) : IRequest<Result<SchoolDto>>;

public sealed class CreateSchoolCommandValidator : AbstractValidator<CreateSchoolCommand>
{
    public CreateSchoolCommandValidator()
    {
        RuleFor(command => command.School).NotNull().SetValidator(new SchoolWriteValidator());
    }
}

public sealed class CreateSchoolCommandHandler(
    ISchoolRepository schools,
    IUnitOfWork unitOfWork,
    AutoMapper.IMapper mapper) : IRequestHandler<CreateSchoolCommand, Result<SchoolDto>>
{
    public async Task<Result<SchoolDto>> Handle(CreateSchoolCommand request, CancellationToken cancellationToken)
    {
        var slug = CreateSlug(request.School.Name);
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = $"school-{Guid.NewGuid():N}";
        }
        if (await schools.SlugExistsAsync(slug, null, cancellationToken))
        {
            slug = $"{slug}-{Guid.NewGuid():N}"[..(slug.Length + 9)];
            while (await schools.SlugExistsAsync(slug, null, cancellationToken))
            {
                var nameSlug = CreateSlug(request.School.Name);
                slug = $"{nameSlug}-{Guid.NewGuid():N}"[..(nameSlug.Length + 9)];
            }
        }

        var input = request.School;
        var school = new School(input.Name, slug, input.SchoolType, input.Address, input.City, input.IsPrivate);
        school.Update(input.Name, slug, input.Description, input.SchoolType, input.Address, input.City,
            input.PostalCode, input.Phone, input.Email, input.Website, input.TuitionFrom, input.TuitionTo,
            input.StudentCapacity, input.EstablishedYear, input.IsPrivate, input.LogoUrl, input.IsApproved);
        school.ReplacePhotos(input.Photos.OrderBy(photo => photo.SortOrder).Select(photo => photo.Url));
        await schools.AddAsync(school, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SchoolDto>.Success(mapper.Map<SchoolDto>(school));
    }

    public static string CreateSlug(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(character => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) !=
            System.Globalization.UnicodeCategory.NonSpacingMark);
        var slug = new string(chars.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray());
        return string.Join('-', slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
