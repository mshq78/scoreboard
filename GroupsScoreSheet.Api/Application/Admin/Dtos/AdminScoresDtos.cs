namespace GroupsScoreSheet.Api.Application.Admin.Dtos;

public sealed record AdminCourseScoresDto(
    Guid CourseId,
    string OrganizerCompanyName,
    DateTime HoldingDate,
    AdminScoresRoundDto ActiveRound,
    int TeamCount,
    int EventCount,
    int IndicatorCount,
    int ExpectedScoresPerEvaluator,
    IReadOnlyList<AdminEvaluatorScoresDto> Evaluators
);

public sealed record AdminScoresRoundDto(
    Guid Id,
    int RoundNumber,
    string Status,
    DateTime CreatedAt
);

public sealed record AdminEvaluatorScoresDto(
    Guid EvaluatorProfileId,
    string EvaluatorName,
    string Status,
    DateTime? FirstOpenedAt,
    DateTime? LastSyncedAt,
    DateTime? FinalSyncedAt,
    int SubmittedScoreCount,
    int MissingScoreCount,
    int CommentCount,
    IReadOnlyList<AdminRawScoreDto> Scores,
    IReadOnlyList<AdminRawEventCommentDto> Comments
);

public sealed record AdminRawScoreDto(
    Guid Id,
    Guid TeamId,
    string TeamName,
    int TeamDisplayOrder,
    Guid EventId,
    string EventName,
    int EventDisplayOrder,
    Guid IndicatorId,
    string IndicatorName,
    int IndicatorDisplayOrder,
    int Value,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ClientUpdatedAt
);

public sealed record AdminRawEventCommentDto(
    Guid Id,
    Guid TeamId,
    string TeamName,
    int TeamDisplayOrder,
    Guid EventId,
    string EventName,
    int EventDisplayOrder,
    string? CommentText,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ClientUpdatedAt
);

public sealed record AdminCourseScoresResult(
    bool Success,
    AdminCourseScoresDto? Data,
    IReadOnlyList<string> Errors
);