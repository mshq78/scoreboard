namespace GroupsScoreSheet.Api.Application.Evaluator.Dtos;

public sealed class EvaluatorSyncRequest
{
    public Guid CourseId { get; set; }

    public Guid RoundId { get; set; }

    public bool IsFinalSync { get; set; }

    public DateTime? ClientSyncedAt { get; set; }

    public List<EvaluatorSyncScoreRequest> Scores { get; set; } = new();

    public List<EvaluatorSyncEventCommentRequest> Comments { get; set; } = new();
}

public sealed class EvaluatorSyncScoreRequest
{
    public Guid TeamId { get; set; }

    public Guid EventId { get; set; }

    public Guid IndicatorId { get; set; }

    public int? Value { get; set; }

    public DateTime? ClientUpdatedAt { get; set; }
}

public sealed class EvaluatorSyncEventCommentRequest
{
    public Guid TeamId { get; set; }

    public Guid EventId { get; set; }

    public string? CommentText { get; set; }

    public DateTime? ClientUpdatedAt { get; set; }
}

public sealed record EvaluatorSyncResponseDto(
    bool Success,
    string Status,
    bool IsFinalized,
    DateTime ServerSyncedAt,
    string? RejectedReason,
    IReadOnlyList<string> Errors
);

public sealed record EvaluatorSyncResult(
    bool Success,
    EvaluatorSyncResponseDto? Data,
    string? ErrorCode,
    string? Message,
    IReadOnlyList<string> Errors
);