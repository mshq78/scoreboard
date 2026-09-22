namespace GroupsScoreSheet.Api.Application.Admin.Dtos;

public sealed record AdminScoreSheetDto(
    Guid CourseId,
    string OrganizerCompanyName,
    DateTime HoldingDate,
    AdminScoreSheetRoundDto ActiveRound,
    int EvaluatorCount,
    int EvaluatorWithSubmittedScoreCount,
    bool HasSubmittedScores,
    string? Message,
    IReadOnlyList<AdminScoreSheetEventDto> Events
);

public sealed record AdminScoreSheetRoundDto(
    Guid Id,
    int RoundNumber,
    string Status,
    DateTime CreatedAt
);

public sealed record AdminScoreSheetEventDto(
    Guid EventId,
    string EventName,
    int EventDisplayOrder,
    IReadOnlyList<AdminScoreSheetIndicatorDto> Indicators,
    IReadOnlyList<AdminScoreSheetTeamRowDto> Rows
);

public sealed record AdminScoreSheetIndicatorDto(
    Guid IndicatorId,
    string IndicatorName,
    int IndicatorDisplayOrder
);

public sealed record AdminScoreSheetTeamRowDto(
    Guid TeamId,
    string TeamName,
    int TeamDisplayOrder,
    IReadOnlyList<AdminScoreSheetIndicatorValueDto> IndicatorValues,
    decimal? Total,
    decimal? Average,
    string? CombinedComments,
    IReadOnlyList<AdminScoreSheetCommentLineDto> CommentLines
);

public sealed record AdminScoreSheetIndicatorValueDto(
    Guid IndicatorId,
    string IndicatorName,
    decimal? AverageScore,
    int SubmittedScoreCount,
    IReadOnlyList<AdminScoreSheetEvaluatorScoreDto> EvaluatorScores
);

public sealed record AdminScoreSheetEvaluatorScoreDto(
    Guid EvaluatorProfileId,
    string EvaluatorName,
    int Score
);

public sealed record AdminScoreSheetCommentLineDto(
    string EvaluatorName,
    string CommentText
);

public sealed record AdminScoreSheetResult(
    bool Success,
    AdminScoreSheetDto? Data,
    IReadOnlyList<string> Errors
);

public sealed record AdminScoreSheetExportResult(
    bool Success,
    byte[]? FileBytes,
    string? FileName,
    string? ContentType,
    IReadOnlyList<string> Errors
);