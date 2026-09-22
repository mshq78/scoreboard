using GroupsScoreSheet.Api.Application.Admin.Dtos;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public interface IAdminCourseService
{
    Task<CreateCourseResult> CreateCourseAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CourseListItemDto>> GetCoursesAsync(
        CancellationToken cancellationToken);

    Task<CourseDetailsDto?> GetCourseDetailsAsync(
        Guid courseId,
        CancellationToken cancellationToken);
}