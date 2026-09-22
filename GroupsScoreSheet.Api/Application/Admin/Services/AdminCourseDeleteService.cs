using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Domain.Enums;
using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public sealed class AdminCourseDeleteService : IAdminCourseDeleteService
{
    private readonly AppDbContext _dbContext;

    public AdminCourseDeleteService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DeleteCourseResult> DeleteCourseAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            return new DeleteCourseResult(
                false,
                null,
                new[] { "CourseId is required." });
        }

        var course = await _dbContext.Courses
            .FirstOrDefaultAsync(
                x => x.Id == courseId && x.Status != CourseStatus.Deleted,
                cancellationToken);

        if (course is null)
        {
            return new DeleteCourseResult(
                false,
                null,
                new[] { "Course was not found." });
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var eventIds = await _dbContext.CourseEvents
                .Where(x => x.CourseId == courseId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            course.ActiveRoundId = null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var deletedScoreCount = await _dbContext.Scores
                .Where(x => x.CourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedCommentCount = await _dbContext.EventComments
                .Where(x => x.CourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedSyncLogCount = await _dbContext.SyncLogs
                .Where(x => x.CourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedEvaluatorCount = await _dbContext.EvaluatorProfiles
                .Where(x => x.CourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedRoundCount = await _dbContext.EvaluationRounds
                .Where(x => x.CourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedUploadedExcelFileCount = await _dbContext.UploadedExcelFiles
                .Where(x => x.CourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedIndicatorCount = 0;

            if (eventIds.Count > 0)
            {
                deletedIndicatorCount = await _dbContext.EventIndicators
                    .Where(x => eventIds.Contains(x.CourseEventId))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var deletedEventCount = await _dbContext.CourseEvents
                .Where(x => x.CourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedTeamCount = await _dbContext.CourseTeams
                .Where(x => x.CourseId == courseId)
                .ExecuteDeleteAsync(cancellationToken);

            _dbContext.Courses.Remove(course);

            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var dto = new DeleteCourseDto(
                courseId,
                deletedScoreCount,
                deletedCommentCount,
                deletedSyncLogCount,
                deletedEvaluatorCount,
                deletedRoundCount,
                deletedUploadedExcelFileCount,
                deletedIndicatorCount,
                deletedEventCount,
                deletedTeamCount,
                now);

            return new DeleteCourseResult(
                true,
                dto,
                Array.Empty<string>());
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}