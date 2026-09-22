using ClosedXML.Excel;
using GroupsScoreSheet.Api.Application.Admin.Dtos;
using GroupsScoreSheet.Api.Domain.Entities;
using GroupsScoreSheet.Api.Domain.Enums;
using GroupsScoreSheet.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Application.Admin.Services;

public sealed class AdminScoreSheetService : IAdminScoreSheetService
{
    private const string NoSubmittedScoresMessage =
        "هنوز هیچ امتیازی برای این فرم ثبت نشده است.";

    private readonly AppDbContext _dbContext;

    public AdminScoreSheetService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminScoreSheetResult> GetScoreSheetAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (courseId == Guid.Empty)
        {
            return new AdminScoreSheetResult(
                false,
                null,
                new[] { "CourseId is required." });
        }

        var data = await BuildScoreSheetAsync(courseId, cancellationToken);

        if (!data.Success)
        {
            return new AdminScoreSheetResult(
                false,
                null,
                data.Errors);
        }

        return new AdminScoreSheetResult(
            true,
            data.ScoreSheet,
            Array.Empty<string>());
    }

    public async Task<AdminScoreSheetExportResult> ExportScoreSheetAsync(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var data = await BuildScoreSheetAsync(courseId, cancellationToken);

        if (!data.Success || data.ScoreSheet is null)
        {
            return new AdminScoreSheetExportResult(
                false,
                null,
                null,
                null,
                data.Errors);
        }

        var workbookBytes = BuildExcelWorkbook(data.ScoreSheet, data.RawData);

        var safeCompanyName = MakeSafeFileName(data.ScoreSheet.OrganizerCompanyName);
        var fileName = $"GroupsScoreSheet-{safeCompanyName}-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

        return new AdminScoreSheetExportResult(
            true,
            workbookBytes,
            fileName,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Array.Empty<string>());
    }

    private async Task<BuildScoreSheetDataResult> BuildScoreSheetAsync(
        Guid courseId,
        CancellationToken cancellationToken)
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
            return BuildScoreSheetDataResult.Fail("Course was not found.");
        }

        if (course.ActiveRoundId is null || course.ActiveRound is null)
        {
            return BuildScoreSheetDataResult.Fail("Course does not have an active evaluation round.");
        }

        var teams = course.Teams
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        var events = course.Events
            .OrderBy(x => x.DisplayOrder)
            .ToList();

        var evaluators = await _dbContext.EvaluatorProfiles
    .AsNoTracking()
    .Where(x =>
        x.CourseId == course.Id &&
        x.EvaluationRoundId == course.ActiveRoundId &&
        x.Status != EvaluatorProfileStatus.Invalidated)
    .OrderBy(x => x.CreatedAt)
    .ToListAsync(cancellationToken);

        var evaluatorIds = evaluators
            .Select(x => x.Id)
            .ToHashSet();

        var evaluatorNameMap = evaluators
            .ToDictionary(x => x.Id, x => x.EvaluatorName);

        var scores = evaluatorIds.Count == 0
            ? new List<Score>()
            : await _dbContext.Scores
                .AsNoTracking()
                .Where(x =>
                    x.CourseId == course.Id &&
                    x.EvaluationRoundId == course.ActiveRoundId &&
                    evaluatorIds.Contains(x.EvaluatorProfileId))
                .ToListAsync(cancellationToken);

        var comments = evaluatorIds.Count == 0
            ? new List<EventComment>()
            : await _dbContext.EventComments
                .AsNoTracking()
                .Where(x =>
                    x.CourseId == course.Id &&
                    x.EvaluationRoundId == course.ActiveRoundId &&
                    evaluatorIds.Contains(x.EvaluatorProfileId))
                .ToListAsync(cancellationToken);

        var evaluatorWithSubmittedScoreCount = scores
            .Select(x => x.EvaluatorProfileId)
            .Distinct()
            .Count();

        var hasSubmittedScores = scores.Count > 0;

        var scoreGroupsMap = scores
    .GroupBy(x => new
    {
        x.TeamId,
        x.CourseEventId,
        x.EventIndicatorId
    })
    .ToDictionary(
        group => (group.Key.TeamId, group.Key.CourseEventId, group.Key.EventIndicatorId),
        group => group.ToList());

        var scoreDetailsMap = scoreGroupsMap
            .ToDictionary(
                item => item.Key,
                item => item.Value
                    .Select(score => new AdminScoreSheetEvaluatorScoreDto(
                        score.EvaluatorProfileId,
                        evaluatorNameMap.TryGetValue(score.EvaluatorProfileId, out var evaluatorName)
                            ? evaluatorName
                            : "ارزیاب",
                        score.Value))
                    .OrderBy(x => x.EvaluatorName)
                    .ToList());

        var commentsMap = comments
    .Where(x => !string.IsNullOrWhiteSpace(x.CommentText))
    .GroupBy(x => new
    {
        x.TeamId,
        x.CourseEventId
    })
    .ToDictionary(
        group => (group.Key.TeamId, group.Key.CourseEventId),
        group => group
            .OrderBy(comment => evaluatorNameMap.TryGetValue(comment.EvaluatorProfileId, out var name)
                ? name
                : string.Empty)
            .Select(comment => new AdminScoreSheetCommentLineDto(
                evaluatorNameMap.TryGetValue(comment.EvaluatorProfileId, out var evaluatorName)
                    ? evaluatorName
                    : "ارزیاب",
                comment.CommentText!.Trim()))
            .ToList());

        var eventDtos = new List<AdminScoreSheetEventDto>();

        foreach (var courseEvent in events)
        {
            var indicators = courseEvent.Indicators
                .OrderBy(x => x.DisplayOrder)
                .ToList();

            var indicatorDtos = indicators
                .Select(indicator => new AdminScoreSheetIndicatorDto(
                    indicator.Id,
                    indicator.Name,
                    indicator.DisplayOrder))
                .ToList();

            var rows = new List<AdminScoreSheetTeamRowDto>();

            foreach (var team in teams)
            {
                var indicatorValues = new List<AdminScoreSheetIndicatorValueDto>();
                decimal total = 0m;
                var indicatorsWithScoreCount = 0;

                foreach (var indicator in indicators)
                {
                    decimal? averageScore = null;
                    var submittedScoreCount = 0;

                    if (scoreGroupsMap.TryGetValue(
                        (team.Id, courseEvent.Id, indicator.Id),
                        out var submittedScoresForCell) &&
                        submittedScoresForCell.Count > 0)
                    {
                        submittedScoreCount = submittedScoresForCell.Count;

                        averageScore = Math.Round(
    submittedScoresForCell.Average(score => (decimal)score.Value),
    2,
    MidpointRounding.AwayFromZero);

                        total += averageScore.Value;
                        indicatorsWithScoreCount++;
                    }

                    scoreDetailsMap.TryGetValue(
                        (team.Id, courseEvent.Id, indicator.Id),
                        out var evaluatorScores);

                    evaluatorScores ??= new List<AdminScoreSheetEvaluatorScoreDto>();

                    indicatorValues.Add(new AdminScoreSheetIndicatorValueDto(
                        indicator.Id,
                        indicator.Name,
                        averageScore,
                        submittedScoreCount,
                        evaluatorScores));
                }
                decimal? totalValue = indicatorsWithScoreCount > 0
                    ? Math.Round(total, 2, MidpointRounding.AwayFromZero)
                    : null;

                decimal? average = null;

                if (indicatorsWithScoreCount > 0)
                {
                    average = Math.Round(
                        total / indicatorsWithScoreCount,
                        2,
                        MidpointRounding.AwayFromZero);
                }

                commentsMap.TryGetValue(
                    (team.Id, courseEvent.Id),
                    out var commentLines);

                commentLines ??= new List<AdminScoreSheetCommentLineDto>();

                var combinedComments = commentLines.Count == 0
                    ? null
                    : string.Join(
                        Environment.NewLine,
                        commentLines.Select(x => $"{x.EvaluatorName}: {x.CommentText}"));

                rows.Add(new AdminScoreSheetTeamRowDto(
                    team.Id,
                    team.Name,
                    team.DisplayOrder,
                    indicatorValues,
                    totalValue,
                    average,
                    combinedComments,
                    commentLines));
            }

            eventDtos.Add(new AdminScoreSheetEventDto(
                courseEvent.Id,
                courseEvent.Name,
                courseEvent.DisplayOrder,
                indicatorDtos,
                rows));
        }

        var scoreSheet = new AdminScoreSheetDto(
            course.Id,
            course.OrganizerCompanyName,
            course.HoldingDate,
            new AdminScoreSheetRoundDto(
                course.ActiveRound.Id,
                course.ActiveRound.RoundNumber,
                course.ActiveRound.Status.ToString(),
                course.ActiveRound.CreatedAt),
            evaluators.Count,
            evaluatorWithSubmittedScoreCount,
            hasSubmittedScores,
            hasSubmittedScores ? null : NoSubmittedScoresMessage,
            eventDtos);

        var rawData = new ScoreSheetRawData(
            evaluators,
            scores,
            comments,
            teams,
            events);

        return BuildScoreSheetDataResult.Ok(scoreSheet, rawData);
    }

    private static byte[] BuildExcelWorkbook(
        AdminScoreSheetDto scoreSheet,
        ScoreSheetRawData rawData)
    {
        using var workbook = new XLWorkbook();

        BuildMainSheet(workbook, scoreSheet);
        BuildEvaluatorsSheet(workbook, rawData.FinalizedEvaluators, scoreSheet);
        BuildRawScoresSheet(workbook, rawData, scoreSheet);
        BuildRawCommentsSheet(workbook, rawData, scoreSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void BuildMainSheet(
        XLWorkbook workbook,
        AdminScoreSheetDto scoreSheet)
    {
        var worksheet = workbook.Worksheets.Add("خروجی ارزیابی");
        worksheet.RightToLeft = true;

        var currentRow = 1;

        worksheet.Cell(currentRow, 1).Value = "خروجی ارزیابی گروه‌ها";
        worksheet.Range(currentRow, 1, currentRow, 8).Merge();
        worksheet.Row(currentRow).Height = 28;
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
        worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.White;

        currentRow += 2;

        worksheet.Cell(currentRow, 1).Value = "شرکت برگزارکننده";
        worksheet.Cell(currentRow, 2).Value = scoreSheet.OrganizerCompanyName;
        worksheet.Cell(currentRow, 4).Value = "تاریخ برگزاری";
        worksheet.Cell(currentRow, 5).Value = scoreSheet.HoldingDate;
        worksheet.Cell(currentRow, 5).Style.DateFormat.Format = "yyyy-mm-dd";
        currentRow++;

        worksheet.Cell(currentRow, 1).Value = "شماره دور";
        worksheet.Cell(currentRow, 2).Value = scoreSheet.ActiveRound.RoundNumber;
        worksheet.Cell(currentRow, 4).Value = "تعداد داورهای دارای امتیاز ثبت‌شده";
        worksheet.Cell(currentRow, 5).Value = scoreSheet.EvaluatorWithSubmittedScoreCount;

        worksheet.Range(currentRow - 1, 1, currentRow, 5).Style.Font.Bold = true;
        worksheet.Range(currentRow - 1, 1, currentRow, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF2F8");

        currentRow += 2;

        if (!scoreSheet.HasSubmittedScores)
        {
            worksheet.Cell(currentRow, 1).Value = NoSubmittedScoresMessage;
            worksheet.Range(currentRow, 1, currentRow, 8).Merge();
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.FromHtml("#9A3412");
            worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEDD5");
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            currentRow += 2;
        }

        foreach (var eventBlock in scoreSheet.Events)
        {
            var indicatorCount = eventBlock.Indicators.Count;
            var lastColumn = 1 + indicatorCount + 3;

            worksheet.Cell(currentRow, 1).Value = eventBlock.EventName;
            worksheet.Range(currentRow, 1, currentRow, lastColumn).Merge();
            worksheet.Row(currentRow).Height = 24;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 13;
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.White;

            currentRow++;

            var headerRow = currentRow;

            worksheet.Cell(headerRow, 1).Value = "گروه / تیم";

            var column = 2;

            foreach (var indicator in eventBlock.Indicators)
            {
                worksheet.Cell(headerRow, column).Value = indicator.IndicatorName;
                column++;
            }

            worksheet.Cell(headerRow, column).Value = "مجموع";
            worksheet.Cell(headerRow, column + 1).Value = "میانگین";
            worksheet.Cell(headerRow, column + 2).Value = "توضیحات ثبت‌شده";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, lastColumn);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9EAF7");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            currentRow++;

            foreach (var teamRow in eventBlock.Rows)
            {
                var dataRow = currentRow;

                worksheet.Cell(dataRow, 1).Value = teamRow.TeamName;

                column = 2;

                foreach (var indicatorValue in teamRow.IndicatorValues)
                {
                    if (indicatorValue.AverageScore is not null)
                    {
                        worksheet.Cell(dataRow, column).Value = indicatorValue.AverageScore.Value;
                    }
                    else
                    {
                        worksheet.Cell(dataRow, column).Value = string.Empty;
                    }

                    column++;
                }

                if (teamRow.Total is not null)
                {
                    worksheet.Cell(dataRow, column).Value = teamRow.Total.Value;
                }

                if (teamRow.Average is not null)
                {
                    worksheet.Cell(dataRow, column + 1).Value = teamRow.Average.Value;
                }

                worksheet.Cell(dataRow, column + 2).Value = teamRow.CombinedComments ?? string.Empty;

                var rowRange = worksheet.Range(dataRow, 1, dataRow, lastColumn);
                rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                if ((teamRow.TeamDisplayOrder % 2) == 0)
                {
                    rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                worksheet.Range(dataRow, 2, dataRow, Math.Max(2, lastColumn - 1))
                    .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell(dataRow, lastColumn).Style.Alignment.WrapText = true;
                worksheet.Row(dataRow).AdjustToContents();

                currentRow++;
            }

            var tableRange = worksheet.Range(headerRow, 1, currentRow - 1, lastColumn);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            worksheet.Range(headerRow + 1, 2, currentRow - 1, lastColumn - 1)
                .Style.NumberFormat.Format = "0.00";

            currentRow += 2;
        }

        worksheet.Columns().AdjustToContents();

        worksheet.Column(1).Width = Math.Max(18, worksheet.Column(1).Width);
        worksheet.Columns(2, 20).Width = 14;

        var usedRange = worksheet.RangeUsed();
        if (usedRange is not null)
        {
            usedRange.Style.Font.FontName = "Calibri";
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        worksheet.SheetView.FreezeRows(1);
    }

    private static void BuildEvaluatorsSheet(
        XLWorkbook workbook,
        IReadOnlyList<EvaluatorProfile> finalizedEvaluators,
        AdminScoreSheetDto scoreSheet)
    {
        var worksheet = workbook.Worksheets.Add("جزئیات داورها");
        worksheet.RightToLeft = true;

        worksheet.Cell(1, 1).Value = "نام داور";
        worksheet.Cell(1, 2).Value = "وضعیت";
        worksheet.Cell(1, 3).Value = "اولین باز شدن لینک";
        worksheet.Cell(1, 4).Value = "آخرین همگام‌سازی";
        worksheet.Cell(1, 5).Value = "ثبت نهایی";

        var row = 2;

        foreach (var evaluator in finalizedEvaluators)
        {
            worksheet.Cell(row, 1).Value = evaluator.EvaluatorName;
            worksheet.Cell(row, 2).Value = evaluator.Status.ToString();
            worksheet.Cell(row, 3).Value = evaluator.FirstOpenedAt;
            worksheet.Cell(row, 4).Value = evaluator.LastSyncedAt;
            worksheet.Cell(row, 5).Value = evaluator.FinalSyncedAt;
            row++;
        }

        if (finalizedEvaluators.Count == 0)
        {
            worksheet.Cell(row, 1).Value = NoSubmittedScoresMessage;
            worksheet.Range(row, 1, row, 5).Merge();
        }

        var usedRange = worksheet.RangeUsed();

        if (usedRange is not null)
        {
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            usedRange.Style.Font.FontName = "Calibri";
        }

        var header = worksheet.Range(1, 1, 1, 5);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Columns().AdjustToContents();
    }

    private static void BuildRawScoresSheet(
        XLWorkbook workbook,
        ScoreSheetRawData rawData,
        AdminScoreSheetDto scoreSheet)
    {
        var worksheet = workbook.Worksheets.Add("Raw Scores");
        worksheet.RightToLeft = true;

        worksheet.Cell(1, 1).Value = "داور";
        worksheet.Cell(1, 2).Value = "گروه";
        worksheet.Cell(1, 3).Value = "رویداد";
        worksheet.Cell(1, 4).Value = "شاخص";
        worksheet.Cell(1, 5).Value = "امتیاز";
        worksheet.Cell(1, 6).Value = "زمان ثبت سمت کلاینت";
        worksheet.Cell(1, 7).Value = "زمان ایجاد در سرور";
        worksheet.Cell(1, 8).Value = "زمان بروزرسانی در سرور";

        var evaluatorMap = rawData.FinalizedEvaluators
            .ToDictionary(x => x.Id, x => x.EvaluatorName);

        var teamMap = rawData.Teams
            .ToDictionary(x => x.Id, x => x.Name);

        var eventMap = rawData.Events
            .ToDictionary(x => x.Id, x => x.Name);

        var indicatorMap = rawData.Events
            .SelectMany(x => x.Indicators)
            .ToDictionary(x => x.Id, x => x.Name);

        var row = 2;

        foreach (var score in rawData.Scores)
        {
            worksheet.Cell(row, 1).Value = evaluatorMap.TryGetValue(score.EvaluatorProfileId, out var evaluatorName)
                ? evaluatorName
                : string.Empty;

            worksheet.Cell(row, 2).Value = teamMap.TryGetValue(score.TeamId, out var teamName)
                ? teamName
                : string.Empty;

            worksheet.Cell(row, 3).Value = eventMap.TryGetValue(score.CourseEventId, out var eventName)
                ? eventName
                : string.Empty;

            worksheet.Cell(row, 4).Value = indicatorMap.TryGetValue(score.EventIndicatorId, out var indicatorName)
                ? indicatorName
                : string.Empty;

            worksheet.Cell(row, 5).Value = score.Value;
            worksheet.Cell(row, 6).Value = score.ClientUpdatedAt;
            worksheet.Cell(row, 7).Value = score.CreatedAt;
            worksheet.Cell(row, 8).Value = score.UpdatedAt;

            row++;
        }

        StyleSimpleTable(worksheet, 8);
    }

    private static void BuildRawCommentsSheet(
        XLWorkbook workbook,
        ScoreSheetRawData rawData,
        AdminScoreSheetDto scoreSheet)
    {
        var worksheet = workbook.Worksheets.Add("Raw Comments");
        worksheet.RightToLeft = true;

        worksheet.Cell(1, 1).Value = "داور";
        worksheet.Cell(1, 2).Value = "گروه";
        worksheet.Cell(1, 3).Value = "رویداد";
        worksheet.Cell(1, 4).Value = "توضیح";
        worksheet.Cell(1, 5).Value = "زمان ثبت سمت کلاینت";
        worksheet.Cell(1, 6).Value = "زمان ایجاد در سرور";
        worksheet.Cell(1, 7).Value = "زمان بروزرسانی در سرور";

        var evaluatorMap = rawData.FinalizedEvaluators
            .ToDictionary(x => x.Id, x => x.EvaluatorName);

        var teamMap = rawData.Teams
            .ToDictionary(x => x.Id, x => x.Name);

        var eventMap = rawData.Events
            .ToDictionary(x => x.Id, x => x.Name);

        var row = 2;

        foreach (var comment in rawData.Comments)
        {
            worksheet.Cell(row, 1).Value = evaluatorMap.TryGetValue(comment.EvaluatorProfileId, out var evaluatorName)
                ? evaluatorName
                : string.Empty;

            worksheet.Cell(row, 2).Value = teamMap.TryGetValue(comment.TeamId, out var teamName)
                ? teamName
                : string.Empty;

            worksheet.Cell(row, 3).Value = eventMap.TryGetValue(comment.CourseEventId, out var eventName)
                ? eventName
                : string.Empty;

            worksheet.Cell(row, 4).Value = comment.CommentText;
            worksheet.Cell(row, 5).Value = comment.ClientUpdatedAt;
            worksheet.Cell(row, 6).Value = comment.CreatedAt;
            worksheet.Cell(row, 7).Value = comment.UpdatedAt;

            row++;
        }

        StyleSimpleTable(worksheet, 7);
        worksheet.Column(4).Width = 50;
        worksheet.Column(4).Style.Alignment.WrapText = true;
    }

    private static void StyleSimpleTable(IXLWorksheet worksheet, int columnCount)
    {
        var usedRange = worksheet.RangeUsed();

        if (usedRange is null)
        {
            return;
        }

        var header = worksheet.Range(1, 1, 1, columnCount);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Font.FontName = "Calibri";

        worksheet.Columns().AdjustToContents();
    }

    private static string MakeSafeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();

        var safe = new string(value
            .Select(ch => invalidChars.Contains(ch) ? '-' : ch)
            .ToArray());

        return string.IsNullOrWhiteSpace(safe)
            ? "Course"
            : safe.Trim();
    }

    private sealed record BuildScoreSheetDataResult(
        bool Success,
        AdminScoreSheetDto? ScoreSheet,
        ScoreSheetRawData RawData,
        IReadOnlyList<string> Errors)
    {
        public static BuildScoreSheetDataResult Ok(
            AdminScoreSheetDto scoreSheet,
            ScoreSheetRawData rawData)
        {
            return new BuildScoreSheetDataResult(
                true,
                scoreSheet,
                rawData,
                Array.Empty<string>());
        }

        public static BuildScoreSheetDataResult Fail(string error)
        {
            return new BuildScoreSheetDataResult(
                false,
                null,
                ScoreSheetRawData.Empty,
                new[] { error });
        }
    }

    private sealed record ScoreSheetRawData(
        IReadOnlyList<EvaluatorProfile> FinalizedEvaluators,
        IReadOnlyList<Score> Scores,
        IReadOnlyList<EventComment> Comments,
        IReadOnlyList<CourseTeam> Teams,
        IReadOnlyList<CourseEvent> Events)
    {
        public static ScoreSheetRawData Empty =>
            new(
                Array.Empty<EvaluatorProfile>(),
                Array.Empty<Score>(),
                Array.Empty<EventComment>(),
                Array.Empty<CourseTeam>(),
                Array.Empty<CourseEvent>());
    }
}
