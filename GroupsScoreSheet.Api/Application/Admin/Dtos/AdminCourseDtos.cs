namespace GroupsScoreSheet.Api.Application.Admin.Dtos;

public sealed record CourseListItemDto(
    Guid Id,
    string OrganizerCompanyName,
    DateTime HoldingDate,
    string Status,
    int TeamCount,
    int EventCount,
    int IndicatorCount,
    int ActiveRoundNumber,
    int EvaluatorCount,
    int FinalizedEvaluatorCount,
    DateTime CreatedAt
);

public sealed record CourseDetailsDto(
    Guid Id,
    string OrganizerCompanyName,
    DateTime HoldingDate,
    string Status,
    EvaluationRoundDto? ActiveRound,
    IReadOnlyList<CourseTeamDto> Teams,
    IReadOnlyList<CourseEventDto> Events,
    CourseEvaluatorSummaryDto EvaluatorSummary,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record CourseTeamDto(
    Guid Id,
    string Name,
    int DisplayOrder
);

public sealed record CourseEventDto(
    Guid Id,
    string Name,
    int DisplayOrder,
    IReadOnlyList<EventIndicatorDto> Indicators
);

public sealed record EventIndicatorDto(
    Guid Id,
    string Name,
    int DisplayOrder
);

public sealed record EvaluationRoundDto(
    Guid Id,
    int RoundNumber,
    string Status,
    DateTime CreatedAt,
    DateTime? ClosedAt
);

public sealed record CourseEvaluatorSummaryDto(
    int TotalEvaluators,
    int ActiveEvaluators,
    int FinalizedEvaluators,
    int InvalidatedEvaluators
);