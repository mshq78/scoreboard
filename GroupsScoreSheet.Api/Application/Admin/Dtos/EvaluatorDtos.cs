namespace GroupsScoreSheet.Api.Application.Admin.Dtos;

public sealed class CreateEvaluatorRequest
{
    public string? EvaluatorName { get; set; }
}

public sealed record EvaluatorProfileDto(
    Guid Id,
    Guid CourseId,
    Guid EvaluationRoundId,
    int RoundNumber,
    string EvaluatorName,
    string EvaluatorToken,
    string EvaluatorUrl,
    string Status,
    DateTime? FirstOpenedAt,
    DateTime? LastSyncedAt,
    DateTime? FinalSyncedAt,
    DateTime CreatedAt
);

public sealed record CreateEvaluatorResult(
    bool Success,
    EvaluatorProfileDto? Evaluator,
    IReadOnlyList<string> Errors
);

public sealed record UnfinalizeEvaluatorResult(
    bool Success,
    EvaluatorProfileDto? Evaluator,
    IReadOnlyList<string> Errors
);