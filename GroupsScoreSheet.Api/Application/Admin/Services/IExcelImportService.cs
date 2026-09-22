using GroupsScoreSheet.Api.Application.Admin.Dtos;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public interface IExcelImportService
{
    Task<ExcelImportResult> ImportEventsAndIndicatorsAsync(
        Stream excelStream,
        CancellationToken cancellationToken);
}