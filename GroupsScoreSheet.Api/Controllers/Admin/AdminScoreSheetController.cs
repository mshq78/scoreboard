using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/courses/{courseId:guid}")]
public sealed class AdminScoreSheetController : ControllerBase
{
    private readonly IAdminScoreSheetService _adminScoreSheetService;

    public AdminScoreSheetController(IAdminScoreSheetService adminScoreSheetService)
    {
        _adminScoreSheetService = adminScoreSheetService;
    }

    [HttpGet("score-sheet")]
    [ProducesResponseType(typeof(AdminScoreSheetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminScoreSheetDto>> GetScoreSheet(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var result = await _adminScoreSheetService.GetScoreSheetAsync(
            courseId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Score sheet could not be loaded.",
                errors = result.Errors
            });
        }

        return Ok(result.Data);
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportScoreSheet(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var result = await _adminScoreSheetService.ExportScoreSheetAsync(
            courseId,
            cancellationToken);

        if (!result.Success || result.FileBytes is null)
        {
            return BadRequest(new
            {
                message = "Score sheet export could not be generated.",
                errors = result.Errors
            });
        }

        return File(
            result.FileBytes,
            result.ContentType!,
            result.FileName);
    }
}