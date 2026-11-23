namespace SchoolWise.Domain.Entities;

public sealed class ReviewContent
{
    private ReviewContent() { }

    public ReviewContent(int overallRating, int teachingRating, int safetyRating, int communicationRating,
        int facilitiesRating, int activitiesRating, string? pros, string? cons, string reviewText)
    {
        OverallRating = overallRating;
        TeachingRating = teachingRating;
        SafetyRating = safetyRating;
        CommunicationRating = communicationRating;
        FacilitiesRating = facilitiesRating;
        ActivitiesRating = activitiesRating;
        Pros = pros?.Trim();
        Cons = cons?.Trim();
        ReviewText = reviewText.Trim();
    }

    public int OverallRating { get; private set; }
    public int TeachingRating { get; private set; }
    public int SafetyRating { get; private set; }
    public int CommunicationRating { get; private set; }
    public int FacilitiesRating { get; private set; }
    public int ActivitiesRating { get; private set; }
    public string? Pros { get; private set; }
    public string? Cons { get; private set; }
    public string ReviewText { get; private set; } = string.Empty;
}
