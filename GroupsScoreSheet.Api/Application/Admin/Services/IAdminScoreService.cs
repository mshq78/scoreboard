using GroupsScoreSheet.Api.Application.Admin.Dtos;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public interface IAdminScoreService
{
    Task<AdminCourseScoresResult> GetCourseScoresAsync(
        Guid courseId,
        CancellationToken cancellationToken);
}