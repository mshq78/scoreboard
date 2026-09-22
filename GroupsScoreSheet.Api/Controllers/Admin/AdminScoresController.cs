using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/courses/{courseId:guid}/scores")]
public sealed class AdminScoresController : ControllerBase
{
    private readonly IAdminScoreService _adminScoreService;

    public AdminScoresController(IAdminScoreService adminScoreService)
    {
        _adminScoreService = adminScoreService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminCourseScoresDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminCourseScoresDto>> GetCourseScores(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var result = await _adminScoreService.GetCourseScoresAsync(
            courseId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Course scores could not be loaded.",
                errors = result.Errors
            });
        }

        return Ok(result.Data);
    }
}