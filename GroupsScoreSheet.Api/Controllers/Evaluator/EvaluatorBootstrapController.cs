using GroupsScoreSheet.Api.Application.Evaluator.Dtos;
using GroupsScoreSheet.Api.Application.Evaluator.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Controllers.Evaluator;

[ApiController]
[Route("api/evaluator/{token}/bootstrap")]
public sealed class EvaluatorBootstrapController : ControllerBase
{
    private readonly IEvaluatorBootstrapService _evaluatorBootstrapService;

    public EvaluatorBootstrapController(
        IEvaluatorBootstrapService evaluatorBootstrapService)
    {
        _evaluatorBootstrapService = evaluatorBootstrapService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(EvaluatorBootstrapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<EvaluatorBootstrapDto>> GetBootstrap(
        string token,
        CancellationToken cancellationToken)
    {
        var result = await _evaluatorBootstrapService.GetBootstrapAsync(
            token,
            cancellationToken);

        if (result.Success)
        {
            return Ok(result.Data);
        }

        return result.ErrorCode switch
        {
            "TOKEN_NOT_FOUND" => NotFound(new
            {
                errorCode = result.ErrorCode,
                message = result.Message
            }),

            "LINK_INVALIDATED" => StatusCode(StatusCodes.Status410Gone, new
            {
                errorCode = result.ErrorCode,
                message = result.Message
            }),

            "STALE_ROUND" => StatusCode(StatusCodes.Status409Conflict, new
            {
                errorCode = result.ErrorCode,
                message = result.Message
            }),

            "ROUND_NOT_ACTIVE" => StatusCode(StatusCodes.Status409Conflict, new
            {
                errorCode = result.ErrorCode,
                message = result.Message
            }),

            "EVALUATOR_ROUND_NOT_ACTIVE" => StatusCode(StatusCodes.Status409Conflict, new
            {
                errorCode = result.ErrorCode,
                message = result.Message
            }),

            _ => BadRequest(new
            {
                errorCode = result.ErrorCode,
                message = result.Message
            })
        };
    }
}