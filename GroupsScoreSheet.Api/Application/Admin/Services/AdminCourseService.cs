using System.Security.Cryptography;
using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Domain.Entities;
using GroupsScoreSheet.Api.Domain.Enums;
using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public sealed class AdminCourseService : IAdminCourseService
{
    private const int MaxTeamCount = 100;
    private const long MaxExcelFileSizeBytes = 10 * 1024 * 1024;

    private readonly AppDbContext _dbContext;
    private readonly IExcelImportService _excelImportService;
    private readonly IWebHostEnvironment _environment;

    public AdminCourseService(
        AppDbContext dbContext,
        IExcelImportService excelImportService,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _excelImportService = excelImportService;
        _environment = environment;
    }

    public async Task<CreateCourseResult> CreateCourseAsync(
        CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateCreateCourseRequest(request);

        if (validationErrors.Count > 0)
        {
            return new CreateCourseResult(false, null, validationErrors);
        }

        var normalizedCompanyName = NormalizeText(request.OrganizerCompanyName);
        var normalizedTeamNames = request.TeamNames
            .Select(NormalizeText)
            .ToList();

        byte[] excelBytes;

        await using (var memoryStream = new MemoryStream())
        {
            await request.ExcelFile!.CopyToAsync(memoryStream, cancellationToken);
            excelBytes = memoryStream.ToArray();
        }

        await using var importStream = new MemoryStream(excelBytes);

        var importResult = await _excelImportService.ImportEventsAndIndicatorsAsync(
            importStream,
            cancellationToken);

        if (!importResult.IsValid)
        {
            var excelErrors = importResult.Errors
                .Select(error => error.ToDisplayMessage())
                .ToList();

            return new CreateCourseResult(false, null, excelErrors);
        }

        var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(request.ExcelFile!.FileName).ToLowerInvariant()}";
        var relativeFilePath = Path.Combine("Storage", "UploadedExcels", storedFileName);
        var physicalDirectoryPath = Path.Combine(_environment.ContentRootPath, "Storage", "UploadedExcels");
        var physicalFilePath = Path.Combine(physicalDirectoryPath, storedFileName);

        Directory.CreateDirectory(physicalDirectoryPath);

        await File.WriteAllBytesAsync(physicalFilePath, excelBytes, cancellationToken);

        var fileHash = ComputeSha256Hash(excelBytes);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var course = new Course
            {
                OrganizerCompanyName = normalizedCompanyName,
                HoldingDate = request.HoldingDate!.Value.Date,
                Status = CourseStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            for (var index = 0; index < normalizedTeamNames.Count; index++)
            {
                course.Teams.Add(new CourseTeam
                {
                    Name = normalizedTeamNames[index],
                    DisplayOrder = index + 1,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var importedEvent in importResult.Events)
            {
                var courseEvent = new CourseEvent
                {
                    Name = importedEvent.Name,
                    DisplayOrder = importedEvent.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var importedIndicator in importedEvent.Indicators)
                {
                    courseEvent.Indicators.Add(new EventIndicator
                    {
                        Name = importedIndicator.Name,
                        DisplayOrder = importedIndicator.DisplayOrder,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                course.Events.Add(courseEvent);
            }

            var initialRound = new EvaluationRound
            {
                RoundNumber = 1,
                Status = EvaluationRoundStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            course.Rounds.Add(initialRound);

            course.UploadedExcelFiles.Add(new UploadedExcelFile
            {
                OriginalFileName = Path.GetFileName(request.ExcelFile!.FileName),
                StoredFileName = storedFileName,
                FilePath = relativeFilePath,
                FileHash = fileHash,
                ImportedAt = DateTime.UtcNow,
                ImportStatus = ExcelImportStatus.Imported,
                ValidationSummary = null
            });

            _dbContext.Courses.Add(course);

            await _dbContext.SaveChangesAsync(cancellationToken);

            course.ActiveRoundId = initialRound.Id;
            course.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var createdDto = new CourseCreatedDto(
                course.Id,
                course.OrganizerCompanyName,
                course.HoldingDate,
                course.Teams.Count,
                course.Events.Count,
                course.Events.Sum(courseEvent => courseEvent.Indicators.Count),
                initialRound.Id,
                initialRound.RoundNumber,
                course.CreatedAt
            );

            return new CreateCourseResult(true, createdDto, Array.Empty<string>());
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            if (File.Exists(physicalFilePath))
            {
                File.Delete(physicalFilePath);
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<CourseListItemDto>> GetCoursesAsync(CancellationToken cancellationToken)
    {
        var courses = await _dbContext.Courses
            .AsNoTracking()
            .Where(course => course.Status != CourseStatus.Deleted)
            .OrderByDescending(course => course.CreatedAt)
            .Select(course => new CourseListItemDto(
                course.Id,
                course.OrganizerCompanyName,
                course.HoldingDate,
                course.Status.ToString(),
                course.Teams.Count,
                course.Events.Count,
                course.Events.SelectMany(courseEvent => courseEvent.Indicators).Count(),
                course.ActiveRound == null ? 0 : course.ActiveRound.RoundNumber,
                _dbContext.EvaluatorProfiles.Count(evaluator =>
                    evaluator.CourseId == course.Id &&
                    evaluator.EvaluationRoundId == course.ActiveRoundId),
                _dbContext.EvaluatorProfiles.Count(evaluator =>
                    evaluator.CourseId == course.Id &&
                    evaluator.EvaluationRoundId == course.ActiveRoundId &&
                    evaluator.Status == EvaluatorProfileStatus.Finalized),
                course.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return courses;
    }

    public async Task<CourseDetailsDto?> GetCourseDetailsAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .AsNoTracking()
            .Include(x => x.ActiveRound)
            .Include(x => x.Teams)
            .Include(x => x.Events)
                .ThenInclude(x => x.Indicators)
            .FirstOrDefaultAsync(
                x => x.Id == courseId && x.Status != CourseStatus.Deleted,
                cancellationToken);

        if (course is null)
        {
            return null;
        }

        var evaluatorSummary = await _dbContext.EvaluatorProfiles
            .AsNoTracking()
            .Where(x =>
                x.CourseId == course.Id &&
                x.EvaluationRoundId == course.ActiveRoundId)
            .GroupBy(_ => 1)
            .Select(group => new CourseEvaluatorSummaryDto(
                group.Count(),
                group.Count(x => x.Status == EvaluatorProfileStatus.Active),
                group.Count(x => x.Status == EvaluatorProfileStatus.Finalized),
                group.Count(x => x.Status == EvaluatorProfileStatus.Invalidated)
            ))
            .FirstOrDefaultAsync(cancellationToken);

        evaluatorSummary ??= new CourseEvaluatorSummaryDto(0, 0, 0, 0);

        return new CourseDetailsDto(
            course.Id,
            course.OrganizerCompanyName,
            course.HoldingDate,
            course.Status.ToString(),
            course.ActiveRound is null
                ? null
                : new EvaluationRoundDto(
                    course.ActiveRound.Id,
                    course.ActiveRound.RoundNumber,
                    course.ActiveRound.Status.ToString(),
                    course.ActiveRound.CreatedAt,
                    course.ActiveRound.ClosedAt),
            course.Teams
                .OrderBy(team => team.DisplayOrder)
                .Select(team => new CourseTeamDto(
                    team.Id,
                    team.Name,
                    team.DisplayOrder))
                .ToList(),
            course.Events
                .OrderBy(courseEvent => courseEvent.DisplayOrder)
                .Select(courseEvent => new CourseEventDto(
                    courseEvent.Id,
                    courseEvent.Name,
                    courseEvent.DisplayOrder,
                    courseEvent.Indicators
                        .OrderBy(indicator => indicator.DisplayOrder)
                        .Select(indicator => new EventIndicatorDto(
                            indicator.Id,
                            indicator.Name,
                            indicator.DisplayOrder))
                        .ToList()))
                .ToList(),
            evaluatorSummary,
            course.CreatedAt,
            course.UpdatedAt
        );
    }

    private static List<string> ValidateCreateCourseRequest(CreateCourseRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.OrganizerCompanyName))
        {
            errors.Add("Organizer company name is required.");
        }

        if (request.HoldingDate is null || request.HoldingDate.Value == default)
        {
            errors.Add("Holding date is required.");
        }

        if (request.TeamCount <= 0)
        {
            errors.Add("Team count must be greater than zero.");
        }

        if (request.TeamCount > MaxTeamCount)
        {
            errors.Add($"Team count cannot be greater than {MaxTeamCount}.");
        }

        if (request.TeamNames is null || request.TeamNames.Count == 0)
        {
            errors.Add("At least one team name is required.");
        }
        else
        {
            if (request.TeamNames.Count != request.TeamCount)
            {
                errors.Add("Team count must match the number of provided team names.");
            }

            var normalizedTeamNames = request.TeamNames
                .Select(NormalizeText)
                .ToList();

            for (var index = 0; index < normalizedTeamNames.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(normalizedTeamNames[index]))
                {
                    errors.Add($"Team name at position {index + 1} is required.");
                }
            }

            var duplicateTeamNames = normalizedTeamNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            foreach (var duplicateTeamName in duplicateTeamNames)
            {
                errors.Add($"Duplicate team name '{duplicateTeamName}' is not allowed.");
            }
        }

        if (request.ExcelFile is null)
        {
            errors.Add("Excel file is required.");
        }
        else
        {
            if (request.ExcelFile.Length == 0)
            {
                errors.Add("Excel file is empty.");
            }

            if (request.ExcelFile.Length > MaxExcelFileSizeBytes)
            {
                errors.Add("Excel file size cannot be greater than 10 MB.");
            }

            var extension = Path.GetExtension(request.ExcelFile.FileName);

            if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Only .xlsx Excel files are supported.");
            }
        }

        return errors;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            ' ',
            value.Trim()
                .Replace('\u00A0', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ComputeSha256Hash(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(bytes);

        return Convert.ToHexString(hashBytes);
    }
}