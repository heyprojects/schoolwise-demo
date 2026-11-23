using FluentValidation;
using MediatR;
using SchoolWise.Application.Common;

namespace SchoolWise.Application.Schools;

public sealed record GetSchoolByIdQuery(Guid Id, bool IncludeUnapproved = false) : IRequest<Result<SchoolDto>>;

public sealed class GetSchoolByIdQueryValidator : AbstractValidator<GetSchoolByIdQuery>
{
    public GetSchoolByIdQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}

public sealed class GetSchoolByIdQueryHandler(
    ISchoolRepository schools,
    AutoMapper.IMapper mapper) : IRequestHandler<GetSchoolByIdQuery, Result<SchoolDto>>
{
    public async Task<Result<SchoolDto>> Handle(GetSchoolByIdQuery request, CancellationToken cancellationToken)
    {
        var school = await schools.GetByIdAsync(request.Id, request.IncludeUnapproved, cancellationToken);
        return school is null
            ? Result<SchoolDto>.Failure("school_not_found", "The requested school was not found.")
            : Result<SchoolDto>.Success(mapper.Map<SchoolDto>(school));
    }
}

public sealed record GetSchoolBySlugQuery(string Slug) : IRequest<Result<SchoolDto>>;

public sealed class GetSchoolBySlugQueryValidator : AbstractValidator<GetSchoolBySlugQuery>
{
    public GetSchoolBySlugQueryValidator() => RuleFor(query => query.Slug).NotEmpty().MaximumLength(220);
}

public sealed class GetSchoolBySlugQueryHandler(
    ISchoolRepository schools,
    AutoMapper.IMapper mapper) : IRequestHandler<GetSchoolBySlugQuery, Result<SchoolDto>>
{
    public async Task<Result<SchoolDto>> Handle(GetSchoolBySlugQuery request, CancellationToken cancellationToken)
    {
        var school = await schools.GetBySlugAsync(request.Slug, cancellationToken);
        return school is null
            ? Result<SchoolDto>.Failure("school_not_found", "The requested school was not found.")
            : Result<SchoolDto>.Success(mapper.Map<SchoolDto>(school));
    }
}
