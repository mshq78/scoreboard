namespace GroupsScoreSheet.Api.Application.Admin.Dtos;

public sealed record DeleteCourseDto(
    Guid CourseId,
    int DeletedScoreCount,
    int DeletedCommentCount,
    int DeletedSyncLogCount,
    int DeletedEvaluatorCount,
    int DeletedRoundCount,
    int DeletedUploadedExcelFileCount,
    int DeletedIndicatorCount,
    int DeletedEventCount,
    int DeletedTeamCount,
    DateTime DeletedAt
);

public sealed record DeleteCourseResult(
    bool Success,
    DeleteCourseDto? Data,
    IReadOnlyList<string> Errors
);