using GroupsScoreSheet.Api.Application.Evaluator.Dtos;
using GroupsScoreSheet.Api.Domain.Enums;
using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Application.Evaluator.Services;

public sealed class EvaluatorBootstrapService : IEvaluatorBootstrapService
{
    private readonly AppDbContext _dbContext;

    public EvaluatorBootstrapService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EvaluatorBootstrapResult> GetBootstrapAsync(
        string token,
        CancellationToken cancellationToken)
    {
        token = NormalizeToken(token);

        if (string.IsNullOrWhiteSpace(token))
        {
            return Fail(
                "INVALID_TOKEN",
                "Evaluator token is required.");
        }

        var evaluator = await _dbContext.EvaluatorProfiles
            .Include(x => x.EvaluationRound)
            .FirstOrDefaultAsync(
                x => x.EvaluatorToken == token,
                cancellationToken);

        if (evaluator is null)
        {
            return Fail(
                "TOKEN_NOT_FOUND",
                "Evaluator link was not found.");
        }

        var course = await _dbContext.Courses
            .AsNoTracking()
            .Include(x => x.ActiveRound)
            .Include(x => x.Teams)
            .Include(x => x.Events)
                .ThenInclude(x => x.Indicators)
            .FirstOrDefaultAsync(
                x => x.Id == evaluator.CourseId,
                cancellationToken);

        if (course is null || course.Status == CourseStatus.Deleted)
        {
            return Fail(
                "COURSE_NOT_FOUND",
                "Course was not found or has been deleted.");
        }

        if (evaluator.Status == EvaluatorProfileStatus.Invalidated)
        {
            return Fail(
                "LINK_INVALIDATED",
                "This evaluator link is no longer valid. The course may have been reset.");
        }

        if (evaluator.EvaluationRound is null)
        {
            return Fail(
                "ROUND_NOT_FOUND",
                "Evaluation round was not found.");
        }

        if (course.ActiveRoundId != evaluator.EvaluationRoundId)
        {
            return Fail(
                "STALE_ROUND",
                "This evaluator link belongs to an old round and cannot be used.");
        }

        if (course.ActiveRound is null || course.ActiveRound.Status != EvaluationRoundStatus.Active)
        {
            return Fail(
                "ROUND_NOT_ACTIVE",
                "The active evaluation round is not available.");
        }

        if (evaluator.EvaluationRound.Status != EvaluationRoundStatus.Active)
        {
            return Fail(
                "EVALUATOR_ROUND_NOT_ACTIVE",
                "This evaluator link is not connected to an active round.");
        }

        if (evaluator.FirstOpenedAt is null)
        {
            evaluator.FirstOpenedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var scores = await _dbContext.Scores
            .AsNoTracking()
            .Where(x =>
                x.EvaluatorProfileId == evaluator.Id &&
                x.CourseId == evaluator.CourseId &&
                x.EvaluationRoundId == evaluator.EvaluationRoundId)
            .Select(x => new EvaluatorScoreBootstrapDto(
                x.Id,
                x.TeamId,
                x.CourseEventId,
                x.EventIndicatorId,
                x.Value,
                x.ClientUpdatedAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

        var comments = await _dbContext.EventComments
            .AsNoTracking()
            .Where(x =>
                x.EvaluatorProfileId == evaluator.Id &&
                x.CourseId == evaluator.CourseId &&
                x.EvaluationRoundId == evaluator.EvaluationRoundId)
            .Select(x => new EvaluatorEventCommentBootstrapDto(
                x.Id,
                x.TeamId,
                x.CourseEventId,
                x.CommentText,
                x.ClientUpdatedAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

        var isReadOnly = evaluator.Status == EvaluatorProfileStatus.Finalized;

        var data = new EvaluatorBootstrapDto(
            new EvaluatorProfileBootstrapDto(
                evaluator.Id,
                evaluator.EvaluatorName,
                evaluator.EvaluatorToken,
                evaluator.Status.ToString(),
                evaluator.FirstOpenedAt,
                evaluator.LastSyncedAt,
                evaluator.FinalSyncedAt),
            new EvaluatorCourseBootstrapDto(
                course.Id,
                course.OrganizerCompanyName,
                course.HoldingDate,
                course.Status.ToString()),
            new EvaluationRoundBootstrapDto(
                evaluator.EvaluationRound.Id,
                evaluator.EvaluationRound.RoundNumber,
                evaluator.EvaluationRound.Status.ToString(),
                evaluator.EvaluationRound.CreatedAt),
            course.Teams
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new EvaluatorTeamBootstrapDto(
                    x.Id,
                    x.Name,
                    x.DisplayOrder))
                .ToList(),
            course.Events
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new EvaluatorEventBootstrapDto(
                    x.Id,
                    x.Name,
                    x.DisplayOrder,
                    x.Indicators
                        .OrderBy(indicator => indicator.DisplayOrder)
                        .Select(indicator => new EvaluatorIndicatorBootstrapDto(
                            indicator.Id,
                            indicator.Name,
                            indicator.DisplayOrder))
                        .ToList()))
                .ToList(),
            scores,
            comments,
            isReadOnly,
            evaluator.Status.ToString());

        return new EvaluatorBootstrapResult(
            true,
            data,
            null,
            null);
    }

    private static EvaluatorBootstrapResult Fail(string errorCode, string message)
    {
        return new EvaluatorBootstrapResult(
            false,
            null,
            errorCode,
            message);
    }

    private static string NormalizeToken(string? token)
    {
        return string.IsNullOrWhiteSpace(token)
            ? string.Empty
            : token.Trim();
    }
}