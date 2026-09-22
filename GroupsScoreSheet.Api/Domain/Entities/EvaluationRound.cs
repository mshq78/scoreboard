using GroupsScoreSheet.Api.Domain.Enums;

namespace GroupsScoreSheet.Api.Domain.Entities;

public sealed class EvaluationRound
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public int RoundNumber { get; set; }

    public EvaluationRoundStatus Status { get; set; } = EvaluationRoundStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAt { get; set; }

    public string? ResetReason { get; set; }

    public ICollection<EvaluatorProfile> EvaluatorProfiles { get; set; } = new List<EvaluatorProfile>();
}