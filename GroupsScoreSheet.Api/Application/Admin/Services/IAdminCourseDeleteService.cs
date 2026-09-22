using GroupsScoreSheet.Api.Application.Admin.Dtos;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public interface IAdminCourseDeleteService
{
    Task<DeleteCourseResult> DeleteCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken);
}