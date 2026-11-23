using SchoolWise.Application.Reviews;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Tests;

public sealed class ReviewTests
{
    [Fact]
    public void EditingPublishedContentRequiresModerationAndPreservesEarlierContent()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), Content("The original classroom experience was welcoming."));
        var first = new ReviewVersion(review);
        review.Moderate(1, true);
        first.Moderate(Guid.NewGuid(), true, "Approved");
        first.Withdraw(DateTimeOffset.UtcNow);
        review.Resubmit(Content("Updated experience with clearer weekly communication."));

        Assert.Equal(ReviewStatus.PendingModeration, review.Status);
        Assert.Equal(2, review.Version);
        Assert.Equal("The original classroom experience was welcoming.", first.Content.ReviewText);
        Assert.Equal(ReviewStatus.Published, first.Status);
        Assert.Throws<InvalidOperationException>(() => review.Moderate(1, true));
    }

    [Fact]
    public void DeletedReviewCannotBeApprovedAndResubmissionStartsANewVersion()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), Content("A helpful description of the classroom routine."));
        review.Delete();
        Assert.Throws<InvalidOperationException>(() => review.Moderate(1, true));
        review.Resubmit(Content("A new submission after withdrawing the original."));
        Assert.Null(review.DeletedAt);
        Assert.Equal(2, review.Version);
        Assert.Equal(ReviewStatus.PendingModeration, review.Status);
    }

    [Fact]
    public void AllRatingsMustBeBetweenOneAndFiveAndTextMustBeMeaningfulLength()
    {
        var validator = new ReviewWriteValidator();
        var valid = new ReviewWriteDto(4, 4, 4, 4, 4, 4, null, null, "A helpful description of the school experience.");
        Assert.True(validator.Validate(valid).IsValid);
        Assert.False(validator.Validate(valid with { SafetyRating = 0 }).IsValid);
        Assert.False(validator.Validate(valid with { TeachingRating = 6 }).IsValid);
        Assert.False(validator.Validate(valid with { ReviewText = new string(' ', 30) }).IsValid);
        Assert.False(validator.Validate(valid with { Cons = new string('x', 1001) }).IsValid);
    }

    private static ReviewContent Content(string text) => new(4, 4, 4, 4, 4, 4, "Helpful communication", null, text);
}
