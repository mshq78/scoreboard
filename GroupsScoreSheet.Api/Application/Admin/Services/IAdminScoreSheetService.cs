using GroupsScoreSheet.Api.Application.Admin.Dtos;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public interface IAdminScoreSheetService
{
    Task<AdminScoreSheetResult> GetScoreSheetAsync(
        Guid courseId,
        CancellationToken cancellationToken);

    Task<AdminScoreSheetExportResult> ExportScoreSheetAsync(
        Guid courseId,
        CancellationToken cancellationToken);
}