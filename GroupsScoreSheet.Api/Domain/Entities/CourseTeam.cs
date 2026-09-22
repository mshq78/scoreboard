namespace GroupsScoreSheet.Api.Domain.Entities;

public sealed class CourseTeam
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}