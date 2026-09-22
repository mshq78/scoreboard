using GroupsScoreSheet.Api.Domain.Enums;

namespace GroupsScoreSheet.Api.Domain.Entities;

public sealed class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OrganizerCompanyName { get; set; } = string.Empty;

    public DateTime HoldingDate { get; set; }

    public CourseStatus Status { get; set; } = CourseStatus.Active;

    public Guid? ActiveRoundId { get; set; }

    public EvaluationRound? ActiveRound { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<CourseTeam> Teams { get; set; } = new List<CourseTeam>();

    public ICollection<CourseEvent> Events { get; set; } = new List<CourseEvent>();

    public ICollection<EvaluationRound> Rounds { get; set; } = new List<EvaluationRound>();

    public ICollection<UploadedExcelFile> UploadedExcelFiles { get; set; } = new List<UploadedExcelFile>();
}