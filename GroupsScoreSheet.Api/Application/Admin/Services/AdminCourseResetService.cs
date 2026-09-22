using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Domain.Entities;
using GroupsScoreSheet.Api.Domain.Enums;
using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public sealed class AdminCourseResetService : IAdminCourseResetService
{
    private const int MaxResetReasonLength = 1000;

    private readonly AppDbContext _dbContext;

    public AdminCourseResetService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResetCourseResult> ResetCourseAsync(
        Guid courseId,
        ResetCourseRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (courseId == Guid.Empty)
        {
            errors.Add("CourseId is required.");
        }

        var resetReason = NormalizeText(request.ResetReason);

        if (resetReason is not null && resetReason.Length > MaxResetReasonLength)
        {
            errors.Add($"Reset reason cannot be longer than {MaxResetReasonLength} characters.");
        }

        if (errors.Count > 0)
        {
            return new ResetCourseResult(false, null, errors);
        }

        var course = await _dbContext.Courses
            .Include(x => x.ActiveRound)
            .FirstOrDefaultAsync(
                x => x.Id == courseId && x.Status != CourseStatus.Deleted,
                cancellationToken);

        if (course is null)
        {
            return new ResetCourseResult(
                false,
                null,
                new[] { "Course was not found." });
        }

        if (course.ActiveRoundId is null || course.ActiveRound is null)
        {
            return new ResetCourseResult(
                false,
                null,
                new[] { "Course does not have an active evaluation round." });
        }

        if (course.ActiveRound.Status != EvaluationRoundStatus.Active)
        {
            return new ResetCourseResult(
                false,
                null,
                new[] { "Course active round is not active." });
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var previousRound = course.ActiveRound;
        var previousRoundId = previousRound.Id;
        var previousRoundNumber = previousRound.RoundNumber;

        try
        {
            var deletedScoreCount = await _dbContext.Scores
                .Where(x =>
                    x.CourseId == course.Id &&
                    x.EvaluationRoundId == previousRoundId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedCommentCount = await _dbContext.EventComments
                .Where(x =>
                    x.CourseId == course.Id &&
                    x.EvaluationRoundId == previousRoundId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedSyncLogCount = await _dbContext.SyncLogs
                .Where(x =>
                    x.CourseId == course.Id &&
                    x.EvaluationRoundId == previousRoundId)
                .ExecuteDeleteAsync(cancellationToken);

            var deletedEvaluatorCount = await _dbContext.EvaluatorProfiles
                .Where(x =>
                    x.CourseId == course.Id &&
                    x.EvaluationRoundId == previousRoundId)
                .ExecuteDeleteAsync(cancellationToken);

            previousRound.Status = EvaluationRoundStatus.Reset;
            previousRound.ClosedAt = now;
            previousRound.ResetReason = resetReason;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var maxRoundNumber = await _dbContext.EvaluationRounds
                .Where(x => x.CourseId == course.Id)
                .MaxAsync(x => x.RoundNumber, cancellationToken);

            var newRound = new EvaluationRound
            {
                CourseId = course.Id,
                RoundNumber = maxRoundNumber + 1,
                Status = EvaluationRoundStatus.Active,
                CreatedAt = now
            };

            _dbContext.EvaluationRounds.Add(newRound);

            await _dbContext.SaveChangesAsync(cancellationToken);

            course.ActiveRoundId = newRound.Id;
            course.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var deletedRoundCount = await _dbContext.EvaluationRounds
                .Where(x => x.Id == previousRoundId)
                .ExecuteDeleteAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var dto = new ResetCourseDto(
                course.Id,
                previousRoundId,
                previousRoundNumber,
                newRound.Id,
                newRound.RoundNumber,
                deletedEvaluatorCount,
                deletedScoreCount,
                deletedCommentCount,
                deletedSyncLogCount,
                deletedRoundCount,
                now);

            return new ResetCourseResult(
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

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(
            ' ',
            value.Trim()
                .Replace('\u00A0', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}