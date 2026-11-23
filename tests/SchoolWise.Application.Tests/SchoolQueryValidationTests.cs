using SchoolWise.Application.Schools;

namespace SchoolWise.Application.Tests;

public sealed class SchoolQueryValidationTests
{
    [Fact]
    public async Task SearchRejectsTuitionRangeWhenMinimumExceedsMaximum()
    {
        var validator = new SearchSchoolsQueryValidator();
        var result = await validator.ValidateAsync(new SearchSchoolsQuery(MinTuition: 8_000, MaxTuition: 3_000));

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("minTuition", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SearchRejectsPageSizeAboveLimit()
    {
        var validator = new SearchSchoolsQueryValidator();
        var result = await validator.ValidateAsync(new SearchSchoolsQuery(PageSize: 101));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SearchSchoolsQuery.PageSize));
    }

    [Fact]
    public async Task SchoolWriteRejectsNonHttpPhotoUrl()
    {
        var validator = new SchoolWriteValidator();
        var input = new SchoolWriteDto(
            "Demo school", null, Domain.Enums.SchoolType.Kindergarten, "Demo address", "Athens",
            null, null, null, null, null, null, null, null, true, null, false,
            [new SchoolPhotoInput("javascript:alert(1)", 0)]);

        var result = await validator.ValidateAsync(input);

        Assert.Contains(result.Errors, error => error.PropertyName.Contains("Photos", StringComparison.Ordinal));
    }
}
