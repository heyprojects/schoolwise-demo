using FluentValidation;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Schools;

public sealed class SchoolWriteValidator : AbstractValidator<SchoolWriteDto>
{
    public SchoolWriteValidator()
    {
        RuleFor(input => input.Name).NotEmpty().MaximumLength(200);
        RuleFor(input => input.Description).MaximumLength(5000);
        RuleFor(input => input.SchoolType).IsInEnum();
        RuleFor(input => input.Address).NotEmpty().MaximumLength(300);
        RuleFor(input => input.City).NotEmpty().MaximumLength(120);
        RuleFor(input => input.PostalCode).MaximumLength(16);
        RuleFor(input => input.Phone).MaximumLength(32);
        RuleFor(input => input.Email).EmailAddress().When(input => !string.IsNullOrWhiteSpace(input.Email));
        RuleFor(input => input.Email).MaximumLength(320);
        RuleFor(input => input.Website).MaximumLength(500).Must(IsWebUrl).When(input => !string.IsNullOrWhiteSpace(input.Website));
        RuleFor(input => input.LogoUrl).MaximumLength(1000).Must(IsWebUrl).When(input => !string.IsNullOrWhiteSpace(input.LogoUrl));
        RuleFor(input => input.TuitionFrom).GreaterThanOrEqualTo(0).When(input => input.TuitionFrom.HasValue);
        RuleFor(input => input.TuitionTo).GreaterThanOrEqualTo(0).When(input => input.TuitionTo.HasValue);
        RuleFor(input => input).Must(input => !input.TuitionFrom.HasValue || !input.TuitionTo.HasValue || input.TuitionFrom <= input.TuitionTo)
            .WithMessage("TuitionFrom must not exceed TuitionTo.");
        RuleFor(input => input.StudentCapacity).GreaterThan(0).When(input => input.StudentCapacity.HasValue);
        RuleFor(input => input.EstablishedYear).InclusiveBetween(1800, DateTime.UtcNow.Year).When(input => input.EstablishedYear.HasValue);
        RuleFor(input => input.Photos).NotNull().Must(photos => photos is not null && photos.Count <= 30);
        RuleForEach(input => input.Photos).ChildRules(photo =>
        {
            photo.RuleFor(item => item.Url).NotEmpty().MaximumLength(1000).Must(IsWebUrl)
                .WithMessage("Photo URL must be a valid HTTP or HTTPS URL.");
            photo.RuleFor(item => item.SortOrder).GreaterThanOrEqualTo(0);
        });
    }

    private static bool IsWebUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
