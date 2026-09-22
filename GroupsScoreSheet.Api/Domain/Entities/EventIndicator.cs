namespace GroupsScoreSheet.Api.Domain.Entities;

public sealed class EventIndicator
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseEventId { get; set; }

    public CourseEvent? CourseEvent { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}