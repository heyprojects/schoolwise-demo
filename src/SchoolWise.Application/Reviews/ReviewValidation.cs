using FluentValidation;

namespace SchoolWise.Application.Reviews;

public sealed class ReviewWriteValidator : AbstractValidator<ReviewWriteDto>
{
    public ReviewWriteValidator(bool saveAsDraft = false)
    {
        var minimumRating = saveAsDraft ? 0 : 1;
        RuleFor(input => input.OverallRating).InclusiveBetween(minimumRating, 5);
        RuleFor(input => input.TeachingRating).InclusiveBetween(minimumRating, 5);
        RuleFor(input => input.SafetyRating).InclusiveBetween(minimumRating, 5);
        RuleFor(input => input.CommunicationRating).InclusiveBetween(minimumRating, 5);
        RuleFor(input => input.FacilitiesRating).InclusiveBetween(minimumRating, 5);
        RuleFor(input => input.ActivitiesRating).InclusiveBetween(minimumRating, 5);
        RuleFor(input => input.ReviewText).NotNull().MaximumLength(5000)
            .Must(text => saveAsDraft || text is not null && text.Trim().Length >= 20)
            .WithMessage("Write at least 20 characters about your experience.");
        RuleFor(input => input.Pros).MaximumLength(1000);
        RuleFor(input => input.Cons).MaximumLength(1000);
    }
}
