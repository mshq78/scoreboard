using GroupsScoreSheet.Api.Domain.Enums;

namespace GroupsScoreSheet.Api.Domain.Entities;

public sealed class UploadedExcelFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string FileHash { get; set; } = string.Empty;

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public ExcelImportStatus ImportStatus { get; set; } = ExcelImportStatus.Imported;

    public string? ValidationSummary { get; set; }
}