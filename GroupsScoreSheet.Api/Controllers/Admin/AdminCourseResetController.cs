using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/courses/{courseId:guid}/reset")]
public sealed class AdminCourseResetController : ControllerBase
{
    private readonly IAdminCourseResetService _adminCourseResetService;

    public AdminCourseResetController(IAdminCourseResetService adminCourseResetService)
    {
        _adminCourseResetService = adminCourseResetService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResetCourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResetCourseDto>> ResetCourse(
        Guid courseId,
        [FromBody] ResetCourseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminCourseResetService.ResetCourseAsync(
            courseId,
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Course could not be reset.",
                errors = result.Errors
            });
        }

        return Ok(result.Data);
    }
}