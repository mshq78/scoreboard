using GroupsScoreSheet.Api.Application.Evaluator.Dtos;

namespace GroupsScoreSheet.Api.Application.Evaluator.Services;

public interface IEvaluatorSyncService
{
    Task<EvaluatorSyncResult> SyncAsync(
        string token,
        EvaluatorSyncRequest request,
        CancellationToken cancellationToken);
}