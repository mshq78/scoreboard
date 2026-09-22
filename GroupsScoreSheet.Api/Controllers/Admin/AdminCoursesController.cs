using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Application.Admin.Services;
using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/courses")]
public sealed class AdminCoursesController : ControllerBase
{
    private readonly IAdminCourseService _adminCourseService;

    public AdminCoursesController(IAdminCourseService adminCourseService)
    {
        _adminCourseService = adminCourseService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CourseCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CourseCreatedDto>> CreateCourse(
        [FromForm] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminCourseService.CreateCourseAsync(
            request,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new
            {
                message = "Course could not be created.",
                errors = result.Errors
            });
        }

        return CreatedAtAction(
            nameof(GetCourseDetails),
            new { courseId = result.Course!.Id },
            result.Course);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseListItemDto>>> GetCourses(
        CancellationToken cancellationToken)
    {
        var courses = await _adminCourseService.GetCoursesAsync(cancellationToken);

        return Ok(courses);
    }

    [HttpGet("{courseId:guid}")]
    [ProducesResponseType(typeof(CourseDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseDetailsDto>> GetCourseDetails(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var course = await _adminCourseService.GetCourseDetailsAsync(courseId, cancellationToken);

        if (course is null)
        {
            return NotFound(new
            {
                message = "Course was not found."
            });
        }

        return Ok(course);
    }
}