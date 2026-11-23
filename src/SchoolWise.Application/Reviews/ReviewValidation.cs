using FluentValidation;

namespace SchoolWise.Application.Reviews;

public sealed class ReviewWriteValidator : AbstractValidator<ReviewWriteDto>
{
    public ReviewWriteValidator()
    {
        RuleFor(input => input.OverallRating).InclusiveBetween(1, 5);
        RuleFor(input => input.TeachingRating).InclusiveBetween(1, 5);
        RuleFor(input => input.SafetyRating).InclusiveBetween(1, 5);
        RuleFor(input => input.CommunicationRating).InclusiveBetween(1, 5);
        RuleFor(input => input.FacilitiesRating).InclusiveBetween(1, 5);
        RuleFor(input => input.ActivitiesRating).InclusiveBetween(1, 5);
        RuleFor(input => input.ReviewText).NotEmpty().MaximumLength(5000)
            .Must(text => text is not null && text.Trim().Length >= 20).WithMessage("Write at least 20 characters about your experience.");
        RuleFor(input => input.Pros).MaximumLength(1000);
        RuleFor(input => input.Cons).MaximumLength(1000);
    }
}
