using GroupsScoreSheet.Api.Application.Admin.Dtos;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public interface IAdminEvaluatorService
{
    Task<CreateEvaluatorResult> CreateEvaluatorAsync(
        Guid courseId,
        CreateEvaluatorRequest request,
        string appBaseUrl,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EvaluatorProfileDto>> GetEvaluatorsAsync(
        Guid courseId,
        string appBaseUrl,
        CancellationToken cancellationToken);

    Task<UnfinalizeEvaluatorResult> UnfinalizeEvaluatorAsync(
        Guid courseId,
        Guid evaluatorId,
        string appBaseUrl,
        CancellationToken cancellationToken);
}