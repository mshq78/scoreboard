using System.Security.Cryptography;
using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Domain.Entities;
using GroupsScoreSheet.Api.Domain.Enums;
using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public sealed class AdminEvaluatorService : IAdminEvaluatorService
{
    private readonly AppDbContext _dbContext;

    public AdminEvaluatorService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateEvaluatorResult> CreateEvaluatorAsync(
        Guid courseId,
        CreateEvaluatorRequest request,
        string appBaseUrl,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .Include(x => x.ActiveRound)
            .FirstOrDefaultAsync(
                x => x.Id == courseId && x.Status != CourseStatus.Deleted,
                cancellationToken);

        if (course is null)
        {
            return new CreateEvaluatorResult(
                false,
                null,
                new[] { "Course was not found." });
        }

        if (course.ActiveRoundId is null || course.ActiveRound is null)
        {
            return new CreateEvaluatorResult(
                false,
                null,
                new[] { "Course does not have an active evaluation round." });
        }

        if (course.ActiveRound.Status != EvaluationRoundStatus.Active)
        {
            return new CreateEvaluatorResult(
                false,
                null,
                new[] { "Course active round is not active." });
        }

        var evaluatorName = NormalizeText(request.EvaluatorName);

        if (string.IsNullOrWhiteSpace(evaluatorName))
        {
            var evaluatorCount = await _dbContext.EvaluatorProfiles
                .CountAsync(
                    x => x.CourseId == course.Id &&
                         x.EvaluationRoundId == course.ActiveRoundId,
                    cancellationToken);

            evaluatorName = $"Evaluator {evaluatorCount + 1}";
        }

        var token = await GenerateUniqueTokenAsync(cancellationToken);

        var evaluatorProfile = new EvaluatorProfile
        {
            CourseId = course.Id,
            EvaluationRoundId = course.ActiveRound.Id,
            EvaluatorName = evaluatorName,
            EvaluatorToken = token,
            Status = EvaluatorProfileStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.EvaluatorProfiles.Add(evaluatorProfile);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = ToDto(
            evaluatorProfile,
            course.ActiveRound.RoundNumber,
            appBaseUrl);

        return new CreateEvaluatorResult(
            true,
            dto,
            Array.Empty<string>());
    }

    public async Task<IReadOnlyList<EvaluatorProfileDto>> GetEvaluatorsAsync(
        Guid courseId,
        string appBaseUrl,
        CancellationToken cancellationToken)
    {
        var course = await _dbContext.Courses
            .AsNoTracking()
            .Include(x => x.ActiveRound)
            .FirstOrDefaultAsync(
                x => x.Id == courseId && x.Status != CourseStatus.Deleted,
                cancellationToken);

        if (course is null || course.ActiveRoundId is null || course.ActiveRound is null)
        {
            return Array.Empty<EvaluatorProfileDto>();
        }

        var evaluators = await _dbContext.EvaluatorProfiles
            .AsNoTracking()
            .Where(x =>
                x.CourseId == course.Id &&
                x.EvaluationRoundId == course.ActiveRoundId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return evaluators
            .Select(evaluator => ToDto(
                evaluator,
                course.ActiveRound.RoundNumber,
                appBaseUrl))
            .ToList();
    }



    public async Task<UnfinalizeEvaluatorResult> UnfinalizeEvaluatorAsync(
        Guid courseId,
        Guid evaluatorId,
        string appBaseUrl,
        CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            return new UnfinalizeEvaluatorResult(
                false,
                null,
                new[] { "CourseId is required." });
        }

        if (evaluatorId == Guid.Empty)
        {
            return new UnfinalizeEvaluatorResult(
                false,
                null,
                new[] { "EvaluatorId is required." });
        }

        var course = await _dbContext.Courses
            .Include(x => x.ActiveRound)
            .FirstOrDefaultAsync(
                x => x.Id == courseId && x.Status != CourseStatus.Deleted,
                cancellationToken);

        if (course is null)
        {
            return new UnfinalizeEvaluatorResult(
                false,
                null,
                new[] { "Course was not found." });
        }

        if (course.ActiveRoundId is null || course.ActiveRound is null)
        {
            return new UnfinalizeEvaluatorResult(
                false,
                null,
                new[] { "Course does not have an active evaluation round." });
        }

        if (course.ActiveRound.Status != EvaluationRoundStatus.Active)
        {
            return new UnfinalizeEvaluatorResult(
                false,
                null,
                new[] { "Course active round is not active." });
        }

        var evaluator = await _dbContext.EvaluatorProfiles
            .FirstOrDefaultAsync(
                x => x.Id == evaluatorId &&
                     x.CourseId == course.Id &&
                     x.EvaluationRoundId == course.ActiveRoundId,
                cancellationToken);

        if (evaluator is null)
        {
            return new UnfinalizeEvaluatorResult(
                false,
                null,
                new[] { "Evaluator profile was not found for the active round." });
        }

        if (evaluator.Status == EvaluatorProfileStatus.Invalidated)
        {
            return new UnfinalizeEvaluatorResult(
                false,
                null,
                new[] { "Invalidated evaluator links cannot be re-opened." });
        }

        if (evaluator.Status != EvaluatorProfileStatus.Finalized)
        {
            return new UnfinalizeEvaluatorResult(
                false,
                null,
                new[] { "Only finalized evaluator profiles can be unfinalized." });
        }

        evaluator.Status = EvaluatorProfileStatus.Active;
        evaluator.FinalSyncedAt = null;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = ToDto(
            evaluator,
            course.ActiveRound.RoundNumber,
            appBaseUrl);

        return new UnfinalizeEvaluatorResult(
            true,
            dto,
            Array.Empty<string>());
    }

    private async Task<string> GenerateUniqueTokenAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var token = GenerateToken();

            var exists = await _dbContext.EvaluatorProfiles
                .AnyAsync(x => x.EvaluatorToken == token, cancellationToken);

            if (!exists)
            {
                return token;
            }
        }

        throw new InvalidOperationException("Could not generate a unique evaluator token.");
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static EvaluatorProfileDto ToDto(
        EvaluatorProfile evaluatorProfile,
        int roundNumber,
        string appBaseUrl)
    {
        var evaluatorUrl = BuildEvaluatorUrl(
            appBaseUrl,
            evaluatorProfile.EvaluatorToken);

        return new EvaluatorProfileDto(
            evaluatorProfile.Id,
            evaluatorProfile.CourseId,
            evaluatorProfile.EvaluationRoundId,
            roundNumber,
            evaluatorProfile.EvaluatorName,
            evaluatorProfile.EvaluatorToken,
            evaluatorUrl,
            evaluatorProfile.Status.ToString(),
            evaluatorProfile.FirstOpenedAt,
            evaluatorProfile.LastSyncedAt,
            evaluatorProfile.FinalSyncedAt,
            evaluatorProfile.CreatedAt);
    }

    private static string BuildEvaluatorUrl(string appBaseUrl, string evaluatorToken)
    {
        return $"{appBaseUrl.TrimEnd('/')}/evaluate/{evaluatorToken}";
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
}