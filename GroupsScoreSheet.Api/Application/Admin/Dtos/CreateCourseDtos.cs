using Microsoft.AspNetCore.Mvc;

namespace GroupsScoreSheet.Api.Application.Admin.Dtos;

public sealed class CreateCourseRequest
{
    [FromForm(Name = "organizerCompanyName")]
    public string? OrganizerCompanyName { get; set; }

    [FromForm(Name = "holdingDate")]
    public DateTime? HoldingDate { get; set; }

    [FromForm(Name = "teamCount")]
    public int TeamCount { get; set; }

    [FromForm(Name = "teamNames")]
    public List<string> TeamNames { get; set; } = new();

    [FromForm(Name = "excelFile")]
    public IFormFile? ExcelFile { get; set; }
}

public sealed record CourseCreatedDto(
    Guid Id,
    string OrganizerCompanyName,
    DateTime HoldingDate,
    int TeamCount,
    int EventCount,
    int IndicatorCount,
    Guid ActiveRoundId,
    int ActiveRoundNumber,
    DateTime CreatedAt
);

public sealed record CreateCourseResult(
    bool Success,
    CourseCreatedDto? Course,
    IReadOnlyList<string> Errors
);