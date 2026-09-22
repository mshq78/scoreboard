using GroupsScoreSheet.Api.Domain.Enums;

namespace GroupsScoreSheet.Api.Domain.Entities;

public sealed class SyncLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EvaluatorProfileId { get; set; }

    public EvaluatorProfile? EvaluatorProfile { get; set; }

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid EvaluationRoundId { get; set; }

    public EvaluationRound? EvaluationRound { get; set; }

    public SyncType SyncType { get; set; }

    public SyncStatus Status { get; set; }

    public int ReceivedScoresCount { get; set; }

    public int ReceivedCommentsCount { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}