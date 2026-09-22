using GroupsScoreSheet.Api.Application.Evaluator.Dtos;
using GroupsScoreSheet.Api.Application.Evaluator.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Controllers.Evaluator;

[ApiController]
[Route("api/evaluator/{token}/sync")]
public sealed class EvaluatorSyncController : ControllerBase
{
    private readonly IEvaluatorSyncService _evaluatorSyncService;

    public EvaluatorSyncController(IEvaluatorSyncService evaluatorSyncService)
    {
        _evaluatorSyncService = evaluatorSyncService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(EvaluatorSyncResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<EvaluatorSyncResponseDto>> Sync(
        string token,
        [FromBody] EvaluatorSyncRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _evaluatorSyncService.SyncAsync(
            token,
            request,
            cancellationToken);

        if (result.Success)
        {
            return Ok(result.Data);
        }

        var errorResponse = new
        {
            errorCode = result.ErrorCode,
            message = result.Message,
            errors = result.Errors
        };

        return result.ErrorCode switch
        {
            "TOKEN_NOT_FOUND" => NotFound(errorResponse),

            "LINK_INVALIDATED" => StatusCode(StatusCodes.Status410Gone, errorResponse),

            "SYNC_REJECTED" => StatusCode(StatusCodes.Status409Conflict, errorResponse),

            "FINAL_SYNC_INCOMPLETE" => BadRequest(errorResponse),

            "VALIDATION_FAILED" => BadRequest(errorResponse),

            "SYNC_FAILED" => StatusCode(StatusCodes.Status500InternalServerError, errorResponse),

            _ => BadRequest(errorResponse)
        };
    }
}