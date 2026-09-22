using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/courses/{courseId:guid}")]
public sealed class AdminCourseDeleteController : ControllerBase
{
    private readonly IAdminCourseDeleteService _adminCourseDeleteService;

    public AdminCourseDeleteController(IAdminCourseDeleteService adminCourseDeleteService)
    {
        _adminCourseDeleteService = adminCourseDeleteService;
    }

    [HttpDelete]
    [ProducesResponseType(typeof(DeleteCourseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteCourseDto>> DeleteCourse(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var result = await _adminCourseDeleteService.DeleteCourseAsync(
            courseId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Course could not be deleted.",
                errors = result.Errors
            });
        }

        return Ok(result.Data);
    }
}