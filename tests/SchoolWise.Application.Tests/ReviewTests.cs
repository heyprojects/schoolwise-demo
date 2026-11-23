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

    [Fact]
    public void IncompleteDraftValidationIsSeparateFromSubmissionAndStillEnforcesBounds()
    {
        var content = new ReviewWriteDto(0, 0, 0, 0, 0, 0, null, null, "");
        var create = new CreateReviewValidator();
        var edit = new EditReviewValidator();
        Assert.True(create.Validate(new CreateReviewCommand(Guid.NewGuid(), content, SaveAsDraft: true)).IsValid);
        Assert.True(edit.Validate(new EditReviewCommand(Guid.NewGuid(), 1, content, SaveAsDraft: true)).IsValid);
        Assert.False(create.Validate(new CreateReviewCommand(Guid.NewGuid(), content)).IsValid);
        Assert.False(edit.Validate(new EditReviewCommand(Guid.NewGuid(), 1, content)).IsValid);
        var draft = new ReviewWriteValidator(saveAsDraft: true);
        Assert.True(draft.Validate(content with { OverallRating = 5, ReviewText = "Notes" }).IsValid);
        Assert.False(draft.Validate(content with { SafetyRating = -1 }).IsValid);
        Assert.False(draft.Validate(content with { TeachingRating = 6 }).IsValid);
        Assert.False(draft.Validate(content with { ReviewText = new string('x', 5001) }).IsValid);
        Assert.False(draft.Validate(content with { Pros = new string('x', 1001) }).IsValid);
        Assert.False(draft.Validate(content with { Cons = new string('x', 1001) }).IsValid);
    }

    [Fact]
    public void DraftSnapshotsStayPrivateAndCannotBeModeratedBeforeSubmission()
    {
        var review = new Review(Guid.NewGuid(), Guid.NewGuid(), new(0, 0, 0, 0, 0, 0, null, null, ""), saveAsDraft: true);
        var draft = new ReviewVersion(review);
        Assert.Equal(ReviewStatus.Draft, draft.Status);
        Assert.Throws<InvalidOperationException>(() => review.Moderate(1, true));
        Assert.Throws<InvalidOperationException>(() => draft.Moderate(Guid.NewGuid(), true, null));
        review.Resubmit(Content("The completed draft is now ready for moderation."));
        var submitted = new ReviewVersion(review);
        Assert.Equal(2, review.Version);
        Assert.Equal(ReviewStatus.PendingModeration, submitted.Status);
        Assert.Equal(ReviewStatus.Draft, draft.Status);
        Assert.Equal("", draft.Content.ReviewText);
        Assert.Null(draft.ReviewedAt);
    }

    private static ReviewContent Content(string text) => new(4, 4, 4, 4, 4, 4, "Helpful communication", null, text);
}
