using System.Text.RegularExpressions;
using ClosedXML.Excel;
using GroupsScoreSheet.Api.Application.Admin.Dtos;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public sealed class ExcelImportService : IExcelImportService
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private const string UnexpectedExcelProcessingMessage =
        "The Excel file could not be processed. Please check the file format and try again.";

    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(ILogger<ExcelImportService> logger)
    {
        _logger = logger;
    }

    public Task<ExcelImportResult> ImportEventsAndIndicatorsAsync(
        Stream excelStream,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<ExcelImportValidationError>();
        var events = new List<ImportedExcelEventDto>();

        try
        {
            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
            {
                errors.Add(new ExcelImportValidationError(
                    1,
                    null,
                    "Excel file does not contain any worksheet."));

                return Task.FromResult(BuildResult(events, errors));
            }

            var firstCellHeader = NormalizeText(worksheet.Cell(1, 1).GetString());

            if (!IsValidEventHeader(firstCellHeader))
            {
                errors.Add(new ExcelImportValidationError(
                    1,
                    1,
                    "First cell must be the header 'Event Name' or 'نام رویداد'."));
            }

            var lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumnUsed = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (lastRowUsed < 2)
            {
                errors.Add(new ExcelImportValidationError(
                    2,
                    null,
                    "Excel file does not contain any event rows. Data must start from row 2."));

                return Task.FromResult(BuildResult(events, errors));
            }

            if (lastColumnUsed < 2)
            {
                errors.Add(new ExcelImportValidationError(
                    1,
                    null,
                    "Excel file must contain at least one indicator column after the event name column."));

                return Task.FromResult(BuildResult(events, errors));
            }

            var eventNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var displayOrder = 1;

            for (var row = 2; row <= lastRowUsed; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowValues = new Dictionary<int, string>();

                var lastNonEmptyColumnInRow = 0;

                for (var column = 1; column <= lastColumnUsed; column++)
                {
                    var value = NormalizeText(worksheet.Cell(row, column).GetString());
                    rowValues[column] = value;

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        lastNonEmptyColumnInRow = column;
                    }
                }

                if (lastNonEmptyColumnInRow == 0)
                {
                    errors.Add(new ExcelImportValidationError(
                        row,
                        null,
                        "Fully empty rows are not allowed inside the data area. Remove this row or move it after all data."));

                    continue;
                }

                var rowHasError = false;

                var eventName = rowValues[1];

                if (string.IsNullOrWhiteSpace(eventName))
                {
                    errors.Add(new ExcelImportValidationError(
                        row,
                        1,
                        "Event name is required."));

                    rowHasError = true;
                }

                if (!string.IsNullOrWhiteSpace(eventName) && !eventNames.Add(eventName))
                {
                    errors.Add(new ExcelImportValidationError(
                        row,
                        1,
                        $"Duplicate event name '{eventName}' is not allowed inside one course."));

                    rowHasError = true;
                }

                if (lastNonEmptyColumnInRow < 2)
                {
                    errors.Add(new ExcelImportValidationError(
                        row,
                        2,
                        "Each event must have at least one indicator."));

                    rowHasError = true;
                }

                var indicators = new List<ImportedExcelIndicatorDto>();
                var indicatorNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (var column = 2; column <= lastNonEmptyColumnInRow; column++)
                {
                    var indicatorName = rowValues[column];

                    if (string.IsNullOrWhiteSpace(indicatorName))
                    {
                        errors.Add(new ExcelImportValidationError(
                            row,
                            column,
                            "Indicator cell cannot be empty between used indicator columns."));

                        rowHasError = true;
                        continue;
                    }

                    if (!indicatorNames.Add(indicatorName))
                    {
                        errors.Add(new ExcelImportValidationError(
                            row,
                            column,
                            $"Duplicate indicator '{indicatorName}' inside event '{eventName}' is not allowed."));

                        rowHasError = true;
                        continue;
                    }

                    indicators.Add(new ImportedExcelIndicatorDto(
                        indicatorName,
                        column - 1));
                }

                if (!rowHasError)
                {
                    events.Add(new ImportedExcelEventDto(
                        eventName,
                        displayOrder,
                        indicators));

                    displayOrder++;
                }
            }

            if (events.Count == 0 && errors.Count == 0)
            {
                errors.Add(new ExcelImportValidationError(
                    2,
                    null,
                    "No valid event rows were found."));
            }

            return Task.FromResult(BuildResult(events, errors));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while processing the uploaded Excel file.");

            errors.Add(new ExcelImportValidationError(
                1,
                null,
                UnexpectedExcelProcessingMessage));

            return Task.FromResult(BuildResult(events, errors));
        }
    }

    private static ExcelImportResult BuildResult(
        IReadOnlyList<ImportedExcelEventDto> events,
        IReadOnlyList<ExcelImportValidationError> errors)
    {
        return new ExcelImportResult(
            errors.Count == 0,
            events,
            errors);
    }

    private static bool IsValidEventHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Equals("Event Name", StringComparison.OrdinalIgnoreCase)
               || value.Equals("Event", StringComparison.OrdinalIgnoreCase)
               || value.Equals("نام رویداد", StringComparison.OrdinalIgnoreCase)
               || value.Equals("رویداد", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Replace('\u00A0', ' ')
            .Trim();

        normalized = WhitespaceRegex.Replace(normalized, " ");

        return normalized;
    }
}
