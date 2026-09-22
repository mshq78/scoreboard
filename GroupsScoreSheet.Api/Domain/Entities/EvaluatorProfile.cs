using GroupsScoreSheet.Api.Domain.Enums;

namespace GroupsScoreSheet.Api.Domain.Entities;

public sealed class EvaluatorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid EvaluationRoundId { get; set; }

    public EvaluationRound? EvaluationRound { get; set; }

    public string EvaluatorName { get; set; } = string.Empty;

    public string EvaluatorToken { get; set; } = string.Empty;

    public EvaluatorProfileStatus Status { get; set; } = EvaluatorProfileStatus.Active;

    public DateTime? FirstOpenedAt { get; set; }

    public DateTime? LastSyncedAt { get; set; }

    public DateTime? FinalSyncedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Score> Scores { get; set; } = new List<Score>();

    public ICollection<EventComment> EventComments { get; set; } = new List<EventComment>();

    public ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();
}