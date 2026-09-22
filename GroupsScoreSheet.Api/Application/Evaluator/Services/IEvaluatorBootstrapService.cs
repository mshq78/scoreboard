using GroupsScoreSheet.Api.Application.Evaluator.Dtos;

namespace GroupsScoreSheet.Api.Application.Evaluator.Services;

public interface IEvaluatorBootstrapService
{
    Task<EvaluatorBootstrapResult> GetBootstrapAsync(
        string token,
        CancellationToken cancellationToken);
}