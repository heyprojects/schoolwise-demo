using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;
using SchoolWise.Domain.Entities;
using SchoolWise.Domain.Enums;

namespace SchoolWise.Application.Reviews;

public sealed record CreateReviewCommand(Guid SchoolId, ReviewWriteDto Content, bool SaveAsDraft = false) : IRequest<Result<ManagedReviewDto>>;
public sealed record EditReviewCommand(Guid Id, int Version, ReviewWriteDto Content, bool SaveAsDraft = false) : IRequest<Result<ManagedReviewDto>>;
public sealed record DeleteReviewCommand(Guid Id, int Version) : IRequest<Result<bool>>;
public sealed record ModerateReviewCommand(Guid Id, int Version, bool Approve, string? Notes) : IRequest<Result<bool>>;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        RuleFor(command => command.SchoolId).NotEmpty();
        RuleFor(command => command.Content).NotNull().SetValidator(command => new ReviewWriteValidator(command.SaveAsDraft));
    }
}

public sealed class EditReviewValidator : AbstractValidator<EditReviewCommand>
{
    public EditReviewValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Version).GreaterThan(0);
        RuleFor(command => command.Content).NotNull().SetValidator(command => new ReviewWriteValidator(command.SaveAsDraft));
    }
}

public sealed class DeleteReviewValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Version).GreaterThan(0);
    }
}

public sealed class ModerateReviewValidator : AbstractValidator<ModerateReviewCommand>
{
    public ModerateReviewValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Version).GreaterThan(0);
        RuleFor(command => command.Notes).MaximumLength(1000);
        When(command => !command.Approve, () => RuleFor(command => command.Notes)
            .NotEmpty().WithMessage("Provide a rejection reason for the parent."));
    }
}

public sealed class CreateReviewHandler(ICurrentUser actor, IUserRepository users, ISchoolRepository schools,
    IReviewRepository reviews, IUnitOfWork unitOfWork) : IRequestHandler<CreateReviewCommand, Result<ManagedReviewDto>>
{
    public async Task<Result<ManagedReviewDto>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var user = actor.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is not { IsActive: true, Role: UserRole.Parent })
            return Result<ManagedReviewDto>.Failure("forbidden", "An active Parent account is required.");
        var school = await schools.GetByIdAsync(request.SchoolId, false, cancellationToken);
        if (school is null)
            return Result<ManagedReviewDto>.Failure("school_not_found", "School not found.");
        var review = await reviews.GetForParentSchoolAsync(user.Id, school.Id, cancellationToken);
        if (review is not null && review.DeletedAt is null)
            return Result<ManagedReviewDto>.Failure("review_exists", "You already reviewed this school. Edit your review instead.");
        if (review is null)
        {
            review = new Review(school.Id, user.Id, request.Content.ToContent(), request.SaveAsDraft);
            await reviews.AddAsync(review, cancellationToken);
        }
        else
        {
            review.Resubmit(request.Content.ToContent(), request.SaveAsDraft);
        }
        await reviews.AddVersionAsync(new ReviewVersion(review), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ManagedReviewDto>.Success(review.ToDto(school.Name, school.Slug, null));
    }
}

public sealed class EditReviewHandler(ICurrentUser actor, IUserRepository users, IReviewRepository reviews,
    ISchoolRepository schools, IUnitOfWork unitOfWork) : IRequestHandler<EditReviewCommand, Result<ManagedReviewDto>>
{
    public async Task<Result<ManagedReviewDto>> Handle(EditReviewCommand request, CancellationToken cancellationToken)
    {
        var user = actor.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is not { IsActive: true, Role: UserRole.Parent })
            return Result<ManagedReviewDto>.Failure("forbidden", "An active Parent account is required.");
        var review = await reviews.GetAsync(request.Id, cancellationToken);
        if (review is null || review.UserId != user.Id || review.DeletedAt is not null)
            return Result<ManagedReviewDto>.Failure("review_not_found", "Review not found.");
        if (review.Version != request.Version)
            return Result<ManagedReviewDto>.Failure("review_conflict", "This review changed. Reload before editing.");
        var school = await schools.GetByIdAsync(review.SchoolId, false, cancellationToken);
        if (school is null)
            return Result<ManagedReviewDto>.Failure("school_not_found", "School is unavailable. You can still delete your review.");
        var previous = await reviews.GetVersionAsync(review.Id, review.Version, cancellationToken);
        previous.Withdraw(DateTimeOffset.UtcNow);
        review.Resubmit(request.Content.ToContent(), request.SaveAsDraft);
        await reviews.AddVersionAsync(new ReviewVersion(review), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ManagedReviewDto>.Success(review.ToDto(school.Name, school.Slug, null));
    }
}

public sealed class DeleteReviewHandler(ICurrentUser actor, IUserRepository users, IReviewRepository reviews,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteReviewCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var user = actor.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (user is not { IsActive: true, Role: UserRole.Parent })
            return Result<bool>.Failure("forbidden", "An active Parent account is required.");
        var review = await reviews.GetAsync(request.Id, cancellationToken);
        if (review is null || review.UserId != user.Id)
            return Result<bool>.Failure("review_not_found", "Review not found.");
        if (review.Version != request.Version)
            return Result<bool>.Failure("review_conflict", "This review changed. Reload before deleting.");
        if (review.DeletedAt is null)
        {
            review.Delete();
            var version = await reviews.GetVersionAsync(review.Id, review.Version, cancellationToken);
            version.Withdraw(review.DeletedAt!.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Result<bool>.Success(true);
    }
}

public sealed class ModerateReviewHandler(ICurrentUser actor, IUserRepository users, IReviewRepository reviews,
    ISchoolRepository schools, IUnitOfWork unitOfWork) : IRequestHandler<ModerateReviewCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ModerateReviewCommand request, CancellationToken cancellationToken)
    {
        var admin = actor.UserId is { } id ? await users.GetByIdAsync(id, cancellationToken) : null;
        if (admin is not { IsActive: true, Role: UserRole.Admin })
            return Result<bool>.Failure("forbidden", "Administrator access is required.");
        var review = await reviews.GetAsync(request.Id, cancellationToken);
        if (review is null || review.DeletedAt is not null)
            return Result<bool>.Failure("review_not_found", "Review not found.");
        if (review.Version != request.Version || review.Status != ReviewStatus.PendingModeration)
            return Result<bool>.Failure("review_conflict", "The pending version changed. Reload the moderation queue.");
        if (request.Approve && await schools.GetByIdAsync(review.SchoolId, false, cancellationToken) is null)
            return Result<bool>.Failure("school_not_found", "Only reviews of approved schools can be published.");
        var version = await reviews.GetVersionAsync(review.Id, review.Version, cancellationToken);
        review.Moderate(request.Version, request.Approve);
        version.Moderate(admin.Id, request.Approve, request.Notes);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
