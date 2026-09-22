namespace GroupsScoreSheet.Api.Application.Evaluator.Dtos;

public sealed record EvaluatorBootstrapDto(
    EvaluatorProfileBootstrapDto Evaluator,
    EvaluatorCourseBootstrapDto Course,
    EvaluationRoundBootstrapDto Round,
    IReadOnlyList<EvaluatorTeamBootstrapDto> Teams,
    IReadOnlyList<EvaluatorEventBootstrapDto> Events,
    IReadOnlyList<EvaluatorScoreBootstrapDto> Scores,
    IReadOnlyList<EvaluatorEventCommentBootstrapDto> Comments,
    bool IsReadOnly,
    string LinkStatus
);

public sealed record EvaluatorProfileBootstrapDto(
    Guid Id,
    string EvaluatorName,
    string EvaluatorToken,
    string Status,
    DateTime? FirstOpenedAt,
    DateTime? LastSyncedAt,
    DateTime? FinalSyncedAt
);

public sealed record EvaluatorCourseBootstrapDto(
    Guid Id,
    string OrganizerCompanyName,
    DateTime HoldingDate,
    string Status
);

public sealed record EvaluationRoundBootstrapDto(
    Guid Id,
    int RoundNumber,
    string Status,
    DateTime CreatedAt
);

public sealed record EvaluatorTeamBootstrapDto(
    Guid Id,
    string Name,
    int DisplayOrder
);

public sealed record EvaluatorEventBootstrapDto(
    Guid Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<EvaluatorIndicatorBootstrapDto> Indicators
);

public sealed record EvaluatorIndicatorBootstrapDto(
    Guid Id,
    string Name,
    int DisplayOrder
);

public sealed record EvaluatorScoreBootstrapDto(
    Guid Id,
    Guid TeamId,
    Guid EventId,
    Guid IndicatorId,
    int Value,
    DateTime? ClientUpdatedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record EvaluatorEventCommentBootstrapDto(
    Guid Id,
    Guid TeamId,
    Guid EventId,
    string? CommentText,
    DateTime? ClientUpdatedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record EvaluatorBootstrapResult(
    bool Success,
    EvaluatorBootstrapDto? Data,
    string? ErrorCode,
    string? Message
);