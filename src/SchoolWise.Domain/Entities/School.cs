using SchoolWise.Domain.Enums;

namespace SchoolWise.Domain.Entities;

public sealed class School
{
    private School() { }

    public School(
        string name,
        string slug,
        SchoolType schoolType,
        string address,
        string city,
        bool isPrivate)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        Slug = slug;
        SchoolType = schoolType;
        Address = address.Trim();
        City = city.Trim();
        IsPrivate = isPrivate;
        IsApproved = false;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public SchoolType SchoolType { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? PostalCode { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Website { get; private set; }
    public decimal? TuitionFrom { get; private set; }
    public decimal? TuitionTo { get; private set; }
    public int? StudentCapacity { get; private set; }
    public int? EstablishedYear { get; private set; }
    public bool IsPrivate { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsApproved { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public ICollection<SchoolPhoto> Photos { get; private set; } = new List<SchoolPhoto>();

    public void Update(
        string name,
        string slug,
        string? description,
        SchoolType schoolType,
        string address,
        string city,
        string? postalCode,
        string? phone,
        string? email,
        string? website,
        decimal? tuitionFrom,
        decimal? tuitionTo,
        int? studentCapacity,
        int? establishedYear,
        bool isPrivate,
        string? logoUrl,
        bool isApproved)
    {
        Name = name.Trim();
        Slug = slug;
        Description = description;
        SchoolType = schoolType;
        Address = address.Trim();
        City = city.Trim();
        PostalCode = postalCode;
        Phone = phone;
        Email = email;
        Website = website;
        TuitionFrom = tuitionFrom;
        TuitionTo = tuitionTo;
        StudentCapacity = studentCapacity;
        EstablishedYear = establishedYear;
        IsPrivate = isPrivate;
        LogoUrl = logoUrl;
        IsApproved = isApproved;
    }

    public void ReplacePhotos(IEnumerable<string> urls)
    {
        Photos.Clear();
        var index = 0;
        foreach (var url in urls.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            Photos.Add(new SchoolPhoto(Id, url.Trim(), index++));
        }
    }
}
