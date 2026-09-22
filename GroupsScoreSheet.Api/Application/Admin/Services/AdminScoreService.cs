using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Domain.Enums;
using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public sealed class AdminScoreService : IAdminScoreService
{
    private readonly AppDbContext _dbContext;

    public AdminScoreService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminCourseScoresResult> GetCourseScoresAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            return new AdminCourseScoresResult(
                false,
                null,
                new[] { "CourseId is required." });
        }

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
            return new AdminCourseScoresResult(
                false,
                null,
                new[] { "Course was not found." });
        }

        if (course.ActiveRoundId is null || course.ActiveRound is null)
        {
            return new AdminCourseScoresResult(
                false,
                null,
                new[] { "Course does not have an active evaluation round." });
        }

        var teams = course.Teams
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        var events = course.Events
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        var indicators = events
            .SelectMany(courseEvent => courseEvent.Indicators)
            .ToList();

        var expectedScoresPerEvaluator = teams.Count * indicators.Count;

        var teamMap = teams.ToDictionary(x => x.Id, x => x);

        var eventMap = events.ToDictionary(x => x.Id, x => x);

        var indicatorMap = indicators.ToDictionary(x => x.Id, x => x);

        var evaluators = await _dbContext.EvaluatorProfiles
            .AsNoTracking()
            .Where(x =>
                x.CourseId == course.Id &&
                x.EvaluationRoundId == course.ActiveRoundId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var evaluatorIds = evaluators
            .Select(x => x.Id)
            .ToList();

        var scores = await _dbContext.Scores
            .AsNoTracking()
            .Where(x =>
                x.CourseId == course.Id &&
                x.EvaluationRoundId == course.ActiveRoundId &&
                evaluatorIds.Contains(x.EvaluatorProfileId))
            .ToListAsync(cancellationToken);

        var comments = await _dbContext.EventComments
            .AsNoTracking()
            .Where(x =>
                x.CourseId == course.Id &&
                x.EvaluationRoundId == course.ActiveRoundId &&
                evaluatorIds.Contains(x.EvaluatorProfileId))
            .ToListAsync(cancellationToken);

        var scoresByEvaluator = scores
            .GroupBy(x => x.EvaluatorProfileId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var commentsByEvaluator = comments
            .GroupBy(x => x.EvaluatorProfileId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var evaluatorScoreDtos = new List<AdminEvaluatorScoresDto>();

        foreach (var evaluator in evaluators)
        {
            scoresByEvaluator.TryGetValue(evaluator.Id, out var evaluatorScores);
            commentsByEvaluator.TryGetValue(evaluator.Id, out var evaluatorComments);

            evaluatorScores ??= new List<Domain.Entities.Score>();
            evaluatorComments ??= new List<Domain.Entities.EventComment>();

            var rawScores = evaluatorScores
                .Where(score =>
                    teamMap.ContainsKey(score.TeamId) &&
                    eventMap.ContainsKey(score.CourseEventId) &&
                    indicatorMap.ContainsKey(score.EventIndicatorId))
                .Select(score =>
                {
                    var team = teamMap[score.TeamId];
                    var courseEvent = eventMap[score.CourseEventId];
                    var indicator = indicatorMap[score.EventIndicatorId];

                    return new AdminRawScoreDto(
                        score.Id,
                        team.Id,
                        team.Name,
                        team.DisplayOrder,
                        courseEvent.Id,
                        courseEvent.Name,
                        courseEvent.DisplayOrder,
                        indicator.Id,
                        indicator.Name,
                        indicator.DisplayOrder,
                        score.Value,
                        score.CreatedAt,
                        score.UpdatedAt,
                        score.ClientUpdatedAt);
                })
                .OrderBy(x => x.TeamDisplayOrder)
                .ThenBy(x => x.EventDisplayOrder)
                .ThenBy(x => x.IndicatorDisplayOrder)
                .ToList();

            var rawComments = evaluatorComments
                .Where(comment =>
                    teamMap.ContainsKey(comment.TeamId) &&
                    eventMap.ContainsKey(comment.CourseEventId))
                .Select(comment =>
                {
                    var team = teamMap[comment.TeamId];
                    var courseEvent = eventMap[comment.CourseEventId];

                    return new AdminRawEventCommentDto(
                        comment.Id,
                        team.Id,
                        team.Name,
                        team.DisplayOrder,
                        courseEvent.Id,
                        courseEvent.Name,
                        courseEvent.DisplayOrder,
                        comment.CommentText,
                        comment.CreatedAt,
                        comment.UpdatedAt,
                        comment.ClientUpdatedAt);
                })
                .OrderBy(x => x.TeamDisplayOrder)
                .ThenBy(x => x.EventDisplayOrder)
                .ToList();

            var submittedScoreCount = rawScores.Count;
            var missingScoreCount = Math.Max(0, expectedScoresPerEvaluator - submittedScoreCount);

            evaluatorScoreDtos.Add(new AdminEvaluatorScoresDto(
                evaluator.Id,
                evaluator.EvaluatorName,
                evaluator.Status.ToString(),
                evaluator.FirstOpenedAt,
                evaluator.LastSyncedAt,
                evaluator.FinalSyncedAt,
                submittedScoreCount,
                missingScoreCount,
                rawComments.Count,
                rawScores,
                rawComments));
        }

        var result = new AdminCourseScoresDto(
            course.Id,
            course.OrganizerCompanyName,
            course.HoldingDate,
            new AdminScoresRoundDto(
                course.ActiveRound.Id,
                course.ActiveRound.RoundNumber,
                course.ActiveRound.Status.ToString(),
                course.ActiveRound.CreatedAt),
            teams.Count,
            events.Count,
            indicators.Count,
            expectedScoresPerEvaluator,
            evaluatorScoreDtos);

        return new AdminCourseScoresResult(
            true,
            result,
            Array.Empty<string>());
    }
}