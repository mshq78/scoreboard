using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupsScoreSheet.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventIndicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventIndicators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventIndicators_CourseEvents_CourseEventId",
                        column: x => x.CourseEventId,
                        principalTable: "CourseEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizerCompanyName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    HoldingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ActiveRoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseTeams_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResetReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationRounds_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UploadedExcelFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportStatus = table.Column<int>(type: "int", nullable: false),
                    ValidationSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadedExcelFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadedExcelFiles_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EvaluatorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationRoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EvaluatorToken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FirstOpenedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluatorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluatorProfiles_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluatorProfiles_EvaluationRounds_EvaluationRoundId",
                        column: x => x.EvaluationRoundId,
                        principalTable: "EvaluationRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationRoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventComments_CourseEvents_CourseEventId",
                        column: x => x.CourseEventId,
                        principalTable: "CourseEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventComments_CourseTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "CourseTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventComments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventComments_EvaluationRounds_EvaluationRoundId",
                        column: x => x.EvaluationRoundId,
                        principalTable: "EvaluationRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventComments_EvaluatorProfiles_EvaluatorProfileId",
                        column: x => x.EvaluatorProfileId,
                        principalTable: "EvaluatorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationRoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventIndicatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                    table.CheckConstraint("CK_Scores_Value_0_10", "[Value] >= 0 AND [Value] <= 10");
                    table.ForeignKey(
                        name: "FK_Scores_CourseEvents_CourseEventId",
                        column: x => x.CourseEventId,
                        principalTable: "CourseEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scores_CourseTeams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "CourseTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scores_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scores_EvaluationRounds_EvaluationRoundId",
                        column: x => x.EvaluationRoundId,
                        principalTable: "EvaluationRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scores_EvaluatorProfiles_EvaluatorProfileId",
                        column: x => x.EvaluatorProfileId,
                        principalTable: "EvaluatorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Scores_EventIndicators_EventIndicatorId",
                        column: x => x.EventIndicatorId,
                        principalTable: "EventIndicators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationRoundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyncType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReceivedScoresCount = table.Column<int>(type: "int", nullable: false),
                    ReceivedCommentsCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncLogs_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncLogs_EvaluationRounds_EvaluationRoundId",
                        column: x => x.EvaluationRoundId,
                        principalTable: "EvaluationRounds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncLogs_EvaluatorProfiles_EvaluatorProfileId",
                        column: x => x.EvaluatorProfileId,
                        principalTable: "EvaluatorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEvents_CourseId_Name",
                table: "CourseEvents",
                columns: new[] { "CourseId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ActiveRoundId",
                table: "Courses",
                column: "ActiveRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTeams_CourseId_Name",
                table: "CourseTeams",
                columns: new[] { "CourseId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRounds_CourseId_RoundNumber",
                table: "EvaluationRounds",
                columns: new[] { "CourseId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationRounds_CourseId_Status",
                table: "EvaluationRounds",
                columns: new[] { "CourseId", "Status" },
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluatorProfiles_CourseId_EvaluationRoundId",
                table: "EvaluatorProfiles",
                columns: new[] { "CourseId", "EvaluationRoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluatorProfiles_EvaluationRoundId",
                table: "EvaluatorProfiles",
                column: "EvaluationRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluatorProfiles_EvaluatorToken",
                table: "EvaluatorProfiles",
                column: "EvaluatorToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_CourseEventId",
                table: "EventComments",
                column: "CourseEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_CourseId_EvaluationRoundId",
                table: "EventComments",
                columns: new[] { "CourseId", "EvaluationRoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_EvaluationRoundId",
                table: "EventComments",
                column: "EvaluationRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_EvaluatorProfileId_TeamId_CourseEventId",
                table: "EventComments",
                columns: new[] { "EvaluatorProfileId", "TeamId", "CourseEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventComments_TeamId",
                table: "EventComments",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_EventIndicators_CourseEventId_Name",
                table: "EventIndicators",
                columns: new[] { "CourseEventId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_CourseEventId",
                table: "Scores",
                column: "CourseEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_CourseId_EvaluationRoundId",
                table: "Scores",
                columns: new[] { "CourseId", "EvaluationRoundId" });

            migrationBuilder.CreateIndex(
                name: "IX_Scores_EvaluationRoundId",
                table: "Scores",
                column: "EvaluationRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_EvaluatorProfileId_TeamId_CourseEventId_EventIndicatorId",
                table: "Scores",
                columns: new[] { "EvaluatorProfileId", "TeamId", "CourseEventId", "EventIndicatorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_EventIndicatorId",
                table: "Scores",
                column: "EventIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_TeamId",
                table: "Scores",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_CourseId",
                table: "SyncLogs",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_EvaluationRoundId",
                table: "SyncLogs",
                column: "EvaluationRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_EvaluatorProfileId",
                table: "SyncLogs",
                column: "EvaluatorProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadedExcelFiles_CourseId",
                table: "UploadedExcelFiles",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEvents_Courses_CourseId",
                table: "CourseEvents",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_EvaluationRounds_ActiveRoundId",
                table: "Courses",
                column: "ActiveRoundId",
                principalTable: "EvaluationRounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationRounds_Courses_CourseId",
                table: "EvaluationRounds");

            migrationBuilder.DropTable(
                name: "EventComments");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "UploadedExcelFiles");

            migrationBuilder.DropTable(
                name: "CourseTeams");

            migrationBuilder.DropTable(
                name: "EventIndicators");

            migrationBuilder.DropTable(
                name: "EvaluatorProfiles");

            migrationBuilder.DropTable(
                name: "CourseEvents");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "EvaluationRounds");
        }
    }
}
