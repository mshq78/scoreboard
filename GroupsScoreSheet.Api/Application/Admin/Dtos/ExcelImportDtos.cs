namespace GroupsScoreSheet.Api.Application.Admin.Dtos;

public sealed record ImportedExcelEventDto(
    string Name,
    int DisplayOrder,
    IReadOnlyList<ImportedExcelIndicatorDto> Indicators
);

public sealed record ImportedExcelIndicatorDto(
    string Name,
    int DisplayOrder
);

public sealed record ExcelImportValidationError(
    int RowNumber,
    int? ColumnNumber,
    string Message
)
{
    public string ToDisplayMessage()
    {
        return ColumnNumber is null
            ? $"Row {RowNumber}: {Message}"
            : $"Row {RowNumber}, Column {ColumnNumber}: {Message}";
    }
}

public sealed record ExcelImportResult(
    bool IsValid,
    IReadOnlyList<ImportedExcelEventDto> Events,
    IReadOnlyList<ExcelImportValidationError> Errors
);