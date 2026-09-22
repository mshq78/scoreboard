namespace GroupsScoreSheet.Api.Application.Admin.Dtos;

public sealed class ResetCourseRequest
{
    public string? ResetReason { get; set; }
}

public sealed record ResetCourseDto(
    Guid CourseId,
    Guid PreviousRoundId,
    int PreviousRoundNumber,
    Guid NewRoundId,
    int NewRoundNumber,
    int DeletedEvaluatorCount,
    int DeletedScoreCount,
    int DeletedCommentCount,
    int DeletedSyncLogCount,
    int DeletedRoundCount,
    DateTime ResetAt
);

public sealed record ResetCourseResult(
    bool Success,
    ResetCourseDto? Data,
    IReadOnlyList<string> Errors
);