namespace SchoolWise.Domain.Entities;

public sealed class SchoolPhoto
{
    private SchoolPhoto() { }

    public SchoolPhoto(Guid schoolId, string url, int sortOrder)
    {
        Id = Guid.NewGuid();
        SchoolId = schoolId;
        Url = url;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public School School { get; private set; } = null!;
}
