namespace GroupsScoreSheet.Api.Domain.Entities;

public sealed class Score
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EvaluatorProfileId { get; set; }

    public EvaluatorProfile? EvaluatorProfile { get; set; }

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid EvaluationRoundId { get; set; }

    public EvaluationRound? EvaluationRound { get; set; }

    public Guid TeamId { get; set; }

    public CourseTeam? Team { get; set; }

    public Guid CourseEventId { get; set; }

    public CourseEvent? CourseEvent { get; set; }

    public Guid EventIndicatorId { get; set; }

    public EventIndicator? EventIndicator { get; set; }

    public int Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ClientUpdatedAt { get; set; }
}