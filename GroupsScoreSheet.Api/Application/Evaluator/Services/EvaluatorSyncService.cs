using GroupsScoreSheet.Api.Application.Evaluator.Dtos;
using GroupsScoreSheet.Api.Domain.Entities;
using GroupsScoreSheet.Api.Domain.Enums;
using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Application.Evaluator.Services;

public sealed class EvaluatorSyncService : IEvaluatorSyncService
{
    private const int MaxCommentLength = 2000;
    private const string UnexpectedSyncErrorMessage =
        "The evaluation data could not be synchronized. Please try again.";

    private readonly AppDbContext _dbContext;
    private readonly ILogger<EvaluatorSyncService> _logger;

    public EvaluatorSyncService(
        AppDbContext dbContext,
        ILogger<EvaluatorSyncService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<EvaluatorSyncResult> SyncAsync(
        string token,
        EvaluatorSyncRequest request,
        CancellationToken cancellationToken)
    {
        token = NormalizeToken(token);

        if (string.IsNullOrWhiteSpace(token))
        {
            return Fail(
                "INVALID_TOKEN",
                "Evaluator token is required.",
                new[] { "Evaluator token is required." });
        }

        if (request is null)
        {
            return Fail(
                "INVALID_REQUEST",
                "Request body is required.",
                new[] { "Request body is required." });
        }

        var syncType = request.IsFinalSync
            ? SyncType.Final
            : SyncType.Partial;

        var evaluator = await _dbContext.EvaluatorProfiles
            .Include(x => x.EvaluationRound)
            .FirstOrDefaultAsync(
                x => x.EvaluatorToken == token,
                cancellationToken);

        if (evaluator is null)
        {
            return Fail(
                "TOKEN_NOT_FOUND",
                "Evaluator link was not found.",
                new[] { "Evaluator link was not found." });
        }

        var course = await _dbContext.Courses
            .Include(x => x.ActiveRound)
            .FirstOrDefaultAsync(
                x => x.Id == evaluator.CourseId && x.Status != CourseStatus.Deleted,
                cancellationToken);

        if (course is null)
        {
            await AddSyncLogAsync(
                evaluator,
                syncType,
                SyncStatus.Rejected,
                request,
                "Course was not found.",
                cancellationToken);

            return Fail(
                "COURSE_NOT_FOUND",
                "Course was not found.",
                new[] { "Course was not found." });
        }

        var basicErrors = ValidateBasicState(
            evaluator,
            course,
            request);

        if (basicErrors.Count > 0)
        {
            await AddSyncLogAsync(
                evaluator,
                syncType,
                SyncStatus.Rejected,
                request,
                string.Join(" | ", basicErrors),
                cancellationToken);

            return Fail(
                "SYNC_REJECTED",
                "Sync request was rejected.",
                basicErrors);
        }

        var teams = await _dbContext.CourseTeams
            .AsNoTracking()
            .Where(x => x.CourseId == course.Id)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

        var events = await _dbContext.CourseEvents
            .AsNoTracking()
            .Include(x => x.Indicators)
            .Where(x => x.CourseId == course.Id)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync(cancellationToken);

        var validationErrors = ValidateSubmittedData(
            request,
            teams,
            events,
            request.IsFinalSync);

        if (validationErrors.Count > 0)
        {
            await AddSyncLogAsync(
                evaluator,
                syncType,
                SyncStatus.Rejected,
                request,
                string.Join(" | ", validationErrors),
                cancellationToken);

            return Fail(
                "VALIDATION_FAILED",
                "Submitted scores or comments are invalid.",
                validationErrors);
        }

        if (request.IsFinalSync)
        {
            var completenessErrors = await ValidateFinalCompletenessAsync(
                evaluator,
                request,
                teams,
                events,
                cancellationToken);

            if (completenessErrors.Count > 0)
            {
                await AddSyncLogAsync(
                    evaluator,
                    syncType,
                    SyncStatus.Rejected,
                    request,
                    string.Join(" | ", completenessErrors),
                    cancellationToken);

                return Fail(
                    "FINAL_SYNC_INCOMPLETE",
                    "Final sync requires all scores to be completed.",
                    completenessErrors);
            }
        }

        var now = DateTime.UtcNow;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await UpsertScoresAsync(
                evaluator,
                request,
                now,
                cancellationToken);

            await UpsertCommentsAsync(
                evaluator,
                request,
                now,
                cancellationToken);

            evaluator.LastSyncedAt = now;

            if (request.IsFinalSync)
            {
                evaluator.Status = EvaluatorProfileStatus.Finalized;
                evaluator.FinalSyncedAt = now;
            }

            _dbContext.SyncLogs.Add(new SyncLog
            {
                EvaluatorProfileId = evaluator.Id,
                CourseId = evaluator.CourseId,
                EvaluationRoundId = evaluator.EvaluationRoundId,
                SyncType = syncType,
                Status = SyncStatus.Success,
                ReceivedScoresCount = request.Scores.Count,
                ReceivedCommentsCount = request.Comments.Count,
                ErrorMessage = null,
                CreatedAt = now
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var response = new EvaluatorSyncResponseDto(
                true,
                request.IsFinalSync ? "Finalized" : "PartialSynced",
                request.IsFinalSync,
                now,
                null,
                Array.Empty<string>());

            return new EvaluatorSyncResult(
                true,
                response,
                null,
                null,
                Array.Empty<string>());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Unexpected error while synchronizing evaluator data for EvaluatorProfileId {EvaluatorProfileId}, CourseId {CourseId}, RoundId {RoundId}, SyncType {SyncType}.",
                evaluator.Id,
                evaluator.CourseId,
                evaluator.EvaluationRoundId,
                syncType);

            await AddSyncLogAsync(
                evaluator,
                syncType,
                SyncStatus.Failed,
                request,
                UnexpectedSyncErrorMessage,
                cancellationToken);

            return Fail(
                "SYNC_FAILED",
                UnexpectedSyncErrorMessage,
                new[] { UnexpectedSyncErrorMessage });
        }
    }

    private static List<string> ValidateBasicState(
        EvaluatorProfile evaluator,
        Course course,
        EvaluatorSyncRequest request)
    {
        var errors = new List<string>();

        if (evaluator.Status == EvaluatorProfileStatus.Invalidated)
        {
            errors.Add("This evaluator link has been invalidated. The course may have been reset.");
        }

        if (evaluator.Status == EvaluatorProfileStatus.Finalized)
        {
            errors.Add("This evaluator profile has already been finalized and cannot be modified.");
        }

        if (evaluator.EvaluationRound is null)
        {
            errors.Add("Evaluator evaluation round was not found.");
        }

        if (evaluator.EvaluationRound is not null &&
            evaluator.EvaluationRound.Status != EvaluationRoundStatus.Active)
        {
            errors.Add("Evaluator evaluation round is not active.");
        }

        if (course.ActiveRoundId is null)
        {
            errors.Add("Course does not have an active evaluation round.");
        }

        if (course.ActiveRound is null ||
            course.ActiveRound.Status != EvaluationRoundStatus.Active)
        {
            errors.Add("Course active round is not active.");
        }

        if (course.ActiveRoundId != evaluator.EvaluationRoundId)
        {
            errors.Add("This evaluator link belongs to an old round and cannot be used.");
        }

        if (request.CourseId == Guid.Empty)
        {
            errors.Add("CourseId is required.");
        }
        else if (request.CourseId != evaluator.CourseId)
        {
            errors.Add("Payload CourseId does not match evaluator profile CourseId.");
        }

        if (request.RoundId == Guid.Empty)
        {
            errors.Add("RoundId is required.");
        }
        else if (request.RoundId != evaluator.EvaluationRoundId)
        {
            errors.Add("Payload RoundId does not match evaluator profile RoundId.");
        }

        return errors;
    }

    private static List<string> ValidateSubmittedData(
        EvaluatorSyncRequest request,
        IReadOnlyList<CourseTeam> teams,
        IReadOnlyList<CourseEvent> events,
        bool isFinalSync)
    {
        var errors = new List<string>();

        request.Scores ??= new List<EvaluatorSyncScoreRequest>();
        request.Comments ??= new List<EvaluatorSyncEventCommentRequest>();

        var teamIds = teams
            .Select(x => x.Id)
            .ToHashSet();

        var eventMap = events
            .ToDictionary(x => x.Id, x => x);

        var indicatorToEventMap = events
            .SelectMany(courseEvent => courseEvent.Indicators.Select(indicator => new
            {
                EventId = courseEvent.Id,
                IndicatorId = indicator.Id
            }))
            .ToDictionary(x => x.IndicatorId, x => x.EventId);

        var scoreKeys = new HashSet<(Guid TeamId, Guid EventId, Guid IndicatorId)>();

        for (var index = 0; index < request.Scores.Count; index++)
        {
            var score = request.Scores[index];
            var prefix = $"Score #{index + 1}:";

            if (score.TeamId == Guid.Empty)
            {
                errors.Add($"{prefix} TeamId is required.");
            }
            else if (!teamIds.Contains(score.TeamId))
            {
                errors.Add($"{prefix} TeamId does not belong to this course.");
            }

            if (score.EventId == Guid.Empty)
            {
                errors.Add($"{prefix} EventId is required.");
            }
            else if (!eventMap.ContainsKey(score.EventId))
            {
                errors.Add($"{prefix} EventId does not belong to this course.");
            }

            if (score.IndicatorId == Guid.Empty)
            {
                errors.Add($"{prefix} IndicatorId is required.");
            }
            else if (!indicatorToEventMap.TryGetValue(score.IndicatorId, out var actualEventId))
            {
                errors.Add($"{prefix} IndicatorId does not belong to this course.");
            }
            else if (score.EventId != Guid.Empty && actualEventId != score.EventId)
            {
                errors.Add($"{prefix} IndicatorId does not belong to the submitted EventId.");
            }

            if (score.Value is null)
            {
                if (isFinalSync)
                {
                    errors.Add($"{prefix} Value is required for final sync.");
                }
            }
            else if (score.Value < 0 || score.Value > 10)
            {
                errors.Add($"{prefix} Value must be an integer between 0 and 10.");
            }

            var key = (score.TeamId, score.EventId, score.IndicatorId);

            if (!scoreKeys.Add(key))
            {
                errors.Add($"{prefix} Duplicate score key in request.");
            }
        }

        var commentKeys = new HashSet<(Guid TeamId, Guid EventId)>();

        for (var index = 0; index < request.Comments.Count; index++)
        {
            var comment = request.Comments[index];
            var prefix = $"Comment #{index + 1}:";

            if (comment.TeamId == Guid.Empty)
            {
                errors.Add($"{prefix} TeamId is required.");
            }
            else if (!teamIds.Contains(comment.TeamId))
            {
                errors.Add($"{prefix} TeamId does not belong to this course.");
            }

            if (comment.EventId == Guid.Empty)
            {
                errors.Add($"{prefix} EventId is required.");
            }
            else if (!eventMap.ContainsKey(comment.EventId))
            {
                errors.Add($"{prefix} EventId does not belong to this course.");
            }

            var normalizedComment = NormalizeComment(comment.CommentText);

            if (normalizedComment is not null && normalizedComment.Length > MaxCommentLength)
            {
                errors.Add($"{prefix} CommentText cannot be longer than {MaxCommentLength} characters.");
            }

            var key = (comment.TeamId, comment.EventId);

            if (!commentKeys.Add(key))
            {
                errors.Add($"{prefix} Duplicate comment key in request.");
            }
        }

        return errors;
    }

    private async Task<List<string>> ValidateFinalCompletenessAsync(
        EvaluatorProfile evaluator,
        EvaluatorSyncRequest request,
        IReadOnlyList<CourseTeam> teams,
        IReadOnlyList<CourseEvent> events,
        CancellationToken cancellationToken)
    {
        var existingScores = await _dbContext.Scores
            .AsNoTracking()
            .Where(x =>
                x.EvaluatorProfileId == evaluator.Id &&
                x.CourseId == evaluator.CourseId &&
                x.EvaluationRoundId == evaluator.EvaluationRoundId)
            .Select(x => new
            {
                x.TeamId,
                x.CourseEventId,
                x.EventIndicatorId
            })
            .ToListAsync(cancellationToken);

        var completedKeys = existingScores
            .Select(x => (x.TeamId, x.CourseEventId, x.EventIndicatorId))
            .ToHashSet();

        foreach (var score in request.Scores.Where(x => x.Value.HasValue))
        {
            completedKeys.Add((score.TeamId, score.EventId, score.IndicatorId));
        }

        var missingMessages = new List<string>();
        var totalMissing = 0;

        foreach (var team in teams.OrderBy(x => x.DisplayOrder))
        {
            foreach (var courseEvent in events.OrderBy(x => x.DisplayOrder))
            {
                foreach (var indicator in courseEvent.Indicators.OrderBy(x => x.DisplayOrder))
                {
                    var key = (team.Id, courseEvent.Id, indicator.Id);

                    if (completedKeys.Contains(key))
                    {
                        continue;
                    }

                    totalMissing++;

                    if (missingMessages.Count < 30)
                    {
                        missingMessages.Add(
                            $"Missing score: Team '{team.Name}', Event '{courseEvent.Name}', Indicator '{indicator.Name}'.");
                    }
                }
            }
        }

        if (totalMissing > 30)
        {
            missingMessages.Add($"And {totalMissing - 30} more missing scores.");
        }

        return missingMessages;
    }

    private async Task UpsertScoresAsync(
        EvaluatorProfile evaluator,
        EvaluatorSyncRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existingScores = await _dbContext.Scores
            .Where(x =>
                x.EvaluatorProfileId == evaluator.Id &&
                x.CourseId == evaluator.CourseId &&
                x.EvaluationRoundId == evaluator.EvaluationRoundId)
            .ToListAsync(cancellationToken);

        var existingScoreMap = existingScores.ToDictionary(
            x => (x.TeamId, x.CourseEventId, x.EventIndicatorId),
            x => x);

        foreach (var submittedScore in request.Scores)
        {
            var key = (submittedScore.TeamId, submittedScore.EventId, submittedScore.IndicatorId);

            if (submittedScore.Value is null)
            {
                if (existingScoreMap.TryGetValue(key, out var existingScoreToRemove))
                {
                    _dbContext.Scores.Remove(existingScoreToRemove);
                }

                continue;
            }

            if (existingScoreMap.TryGetValue(key, out var existingScore))
            {
                existingScore.Value = submittedScore.Value.Value;
                existingScore.ClientUpdatedAt = submittedScore.ClientUpdatedAt;
                existingScore.UpdatedAt = now;
                continue;
            }

            _dbContext.Scores.Add(new Score
            {
                EvaluatorProfileId = evaluator.Id,
                CourseId = evaluator.CourseId,
                EvaluationRoundId = evaluator.EvaluationRoundId,
                TeamId = submittedScore.TeamId,
                CourseEventId = submittedScore.EventId,
                EventIndicatorId = submittedScore.IndicatorId,
                Value = submittedScore.Value.Value,
                CreatedAt = now,
                UpdatedAt = null,
                ClientUpdatedAt = submittedScore.ClientUpdatedAt
            });
        }
    }

    private async Task UpsertCommentsAsync(
        EvaluatorProfile evaluator,
        EvaluatorSyncRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existingComments = await _dbContext.EventComments
            .Where(x =>
                x.EvaluatorProfileId == evaluator.Id &&
                x.CourseId == evaluator.CourseId &&
                x.EvaluationRoundId == evaluator.EvaluationRoundId)
            .ToListAsync(cancellationToken);

        var existingCommentMap = existingComments.ToDictionary(
            x => (x.TeamId, x.CourseEventId),
            x => x);

        foreach (var submittedComment in request.Comments)
        {
            var key = (submittedComment.TeamId, submittedComment.EventId);
            var normalizedComment = NormalizeComment(submittedComment.CommentText);

            if (string.IsNullOrWhiteSpace(normalizedComment))
            {
                if (existingCommentMap.TryGetValue(key, out var existingCommentToRemove))
                {
                    _dbContext.EventComments.Remove(existingCommentToRemove);
                }

                continue;
            }

            if (existingCommentMap.TryGetValue(key, out var existingComment))
            {
                existingComment.CommentText = normalizedComment;
                existingComment.ClientUpdatedAt = submittedComment.ClientUpdatedAt;
                existingComment.UpdatedAt = now;
                continue;
            }

            _dbContext.EventComments.Add(new EventComment
            {
                EvaluatorProfileId = evaluator.Id,
                CourseId = evaluator.CourseId,
                EvaluationRoundId = evaluator.EvaluationRoundId,
                TeamId = submittedComment.TeamId,
                CourseEventId = submittedComment.EventId,
                CommentText = normalizedComment,
                CreatedAt = now,
                UpdatedAt = null,
                ClientUpdatedAt = submittedComment.ClientUpdatedAt
            });
        }
    }

    private async Task AddSyncLogAsync(
        EvaluatorProfile evaluator,
        SyncType syncType,
        SyncStatus status,
        EvaluatorSyncRequest request,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        _dbContext.SyncLogs.Add(new SyncLog
        {
            EvaluatorProfileId = evaluator.Id,
            CourseId = evaluator.CourseId,
            EvaluationRoundId = evaluator.EvaluationRoundId,
            SyncType = syncType,
            Status = status,
            ReceivedScoresCount = request?.Scores?.Count ?? 0,
            ReceivedCommentsCount = request?.Comments?.Count ?? 0,
            ErrorMessage = Truncate(errorMessage, 4000),
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static EvaluatorSyncResult Fail(
        string errorCode,
        string message,
        IReadOnlyList<string> errors)
    {
        return new EvaluatorSyncResult(
            false,
            null,
            errorCode,
            message,
            errors);
    }

    private static string NormalizeToken(string? token)
    {
        return string.IsNullOrWhiteSpace(token)
            ? string.Empty
            : token.Trim();
    }

    private static string? NormalizeComment(string? value)
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

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
