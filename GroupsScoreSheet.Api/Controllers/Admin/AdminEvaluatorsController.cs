using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/courses/{courseId:guid}/evaluators")]
public sealed class AdminEvaluatorsController : ControllerBase
{
    private readonly IAdminEvaluatorService _adminEvaluatorService;

    public AdminEvaluatorsController(IAdminEvaluatorService adminEvaluatorService)
    {
        _adminEvaluatorService = adminEvaluatorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EvaluatorProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EvaluatorProfileDto>>> GetEvaluators(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var appBaseUrl = GetAppBaseUrl();

        var evaluators = await _adminEvaluatorService.GetEvaluatorsAsync(
            courseId,
            appBaseUrl,
            cancellationToken);

        return Ok(evaluators);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EvaluatorProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EvaluatorProfileDto>> CreateEvaluator(
        Guid courseId,
        [FromBody] CreateEvaluatorRequest request,
        CancellationToken cancellationToken)
    {
        var appBaseUrl = GetAppBaseUrl();

        var result = await _adminEvaluatorService.CreateEvaluatorAsync(
            courseId,
            request,
            appBaseUrl,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Evaluator profile could not be created.",
                errors = result.Errors
            });
        }

        return CreatedAtAction(
            nameof(GetEvaluators),
            new { courseId },
            result.Evaluator);
    }



    [HttpPost("{evaluatorId:guid}/unfinalize")]
    [ProducesResponseType(typeof(EvaluatorProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EvaluatorProfileDto>> UnfinalizeEvaluator(
        Guid courseId,
        Guid evaluatorId,
        CancellationToken cancellationToken)
    {
        var appBaseUrl = GetAppBaseUrl();

        var result = await _adminEvaluatorService.UnfinalizeEvaluatorAsync(
            courseId,
            evaluatorId,
            appBaseUrl,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Evaluator profile could not be unfinalized.",
                errors = result.Errors
            });
        }

        return Ok(result.Evaluator);
    }

    private string GetAppBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
    }
}