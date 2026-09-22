using GroupsScoreSheet.Api.Application.Admin.Dtos;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public interface IAdminCourseResetService
{
    Task<ResetCourseResult> ResetCourseAsync(
        Guid courseId,
        ResetCourseRequest request,
        CancellationToken cancellationToken);
}