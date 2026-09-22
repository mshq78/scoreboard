using GroupsScoreSheet.Api.Domain.Entities;
using GroupsScoreSheet.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GroupsScoreSheet.Api.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseTeam> CourseTeams => Set<CourseTeam>();
    public DbSet<CourseEvent> CourseEvents => Set<CourseEvent>();
    public DbSet<EventIndicator> EventIndicators => Set<EventIndicator>();
    public DbSet<EvaluationRound> EvaluationRounds => Set<EvaluationRound>();
    public DbSet<EvaluatorProfile> EvaluatorProfiles => Set<EvaluatorProfile>();
    public DbSet<Score> Scores => Set<Score>();
    public DbSet<EventComment> EventComments => Set<EventComment>();
    public DbSet<UploadedExcelFile> UploadedExcelFiles => Set<UploadedExcelFile>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCourse(modelBuilder);
        ConfigureCourseTeam(modelBuilder);
        ConfigureCourseEvent(modelBuilder);
        ConfigureEventIndicator(modelBuilder);
        ConfigureEvaluationRound(modelBuilder);
        ConfigureEvaluatorProfile(modelBuilder);
        ConfigureScore(modelBuilder);
        ConfigureEventComment(modelBuilder);
        ConfigureUploadedExcelFile(modelBuilder);
        ConfigureSyncLog(modelBuilder);
    }

    private static void ConfigureCourse(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.OrganizerCompanyName)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.HoldingDate)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasOne(x => x.ActiveRound)
                .WithMany()
                .HasForeignKey(x => x.ActiveRoundId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCourseTeam(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseTeam>(entity =>
        {
            entity.ToTable("CourseTeams");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.DisplayOrder)
                .IsRequired();

            entity.HasIndex(x => new { x.CourseId, x.Name })
                .IsUnique();

            entity.HasOne(x => x.Course)
                .WithMany(x => x.Teams)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCourseEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourseEvent>(entity =>
        {
            entity.ToTable("CourseEvents");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.DisplayOrder)
                .IsRequired();

            entity.HasIndex(x => new { x.CourseId, x.Name })
                .IsUnique();

            entity.HasOne(x => x.Course)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEventIndicator(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventIndicator>(entity =>
        {
            entity.ToTable("EventIndicators");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.DisplayOrder)
                .IsRequired();

            entity.HasIndex(x => new { x.CourseEventId, x.Name })
                .IsUnique();

            entity.HasOne(x => x.CourseEvent)
                .WithMany(x => x.Indicators)
                .HasForeignKey(x => x.CourseEventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEvaluationRound(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EvaluationRound>(entity =>
        {
            entity.ToTable("EvaluationRounds");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.RoundNumber)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.ResetReason)
                .HasMaxLength(1000);

            entity.HasIndex(x => new { x.CourseId, x.RoundNumber })
                .IsUnique();

            entity.HasIndex(x => new { x.CourseId, x.Status })
                .HasFilter($"[{nameof(EvaluationRound.Status)}] = {(int)EvaluationRoundStatus.Active}")
                .IsUnique();

            entity.HasOne(x => x.Course)
                .WithMany(x => x.Rounds)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureEvaluatorProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EvaluatorProfile>(entity =>
        {
            entity.ToTable("EvaluatorProfiles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.EvaluatorName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.EvaluatorToken)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.HasIndex(x => x.EvaluatorToken)
                .IsUnique();

            entity.HasIndex(x => new { x.CourseId, x.EvaluationRoundId });

            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EvaluationRound)
                .WithMany(x => x.EvaluatorProfiles)
                .HasForeignKey(x => x.EvaluationRoundId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureScore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Score>(entity =>
        {
            entity.ToTable("Scores", table =>
            {
                table.HasCheckConstraint("CK_Scores_Value_0_10", "[Value] >= 0 AND [Value] <= 10");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Value)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.EvaluatorProfileId,
                x.TeamId,
                x.CourseEventId,
                x.EventIndicatorId
            }).IsUnique();

            entity.HasIndex(x => new { x.CourseId, x.EvaluationRoundId });

            entity.HasOne(x => x.EvaluatorProfile)
                .WithMany(x => x.Scores)
                .HasForeignKey(x => x.EvaluatorProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EvaluationRound)
                .WithMany()
                .HasForeignKey(x => x.EvaluationRoundId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CourseEvent)
                .WithMany()
                .HasForeignKey(x => x.CourseEventId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EventIndicator)
                .WithMany()
                .HasForeignKey(x => x.EventIndicatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureEventComment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventComment>(entity =>
        {
            entity.ToTable("EventComments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CommentText)
                .HasMaxLength(2000);

            entity.HasIndex(x => new
            {
                x.EvaluatorProfileId,
                x.TeamId,
                x.CourseEventId
            }).IsUnique();

            entity.HasIndex(x => new { x.CourseId, x.EvaluationRoundId });

            entity.HasOne(x => x.EvaluatorProfile)
                .WithMany(x => x.EventComments)
                .HasForeignKey(x => x.EvaluatorProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EvaluationRound)
                .WithMany()
                .HasForeignKey(x => x.EvaluationRoundId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CourseEvent)
                .WithMany()
                .HasForeignKey(x => x.CourseEventId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureUploadedExcelFile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UploadedExcelFile>(entity =>
        {
            entity.ToTable("UploadedExcelFiles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.OriginalFileName)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(x => x.StoredFileName)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(x => x.FilePath)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(x => x.FileHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(x => x.ImportStatus)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.ValidationSummary)
                .HasMaxLength(4000);

            entity.HasOne(x => x.Course)
                .WithMany(x => x.UploadedExcelFiles)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSyncLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.ToTable("SyncLogs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.SyncType)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.ErrorMessage)
                .HasMaxLength(4000);

            entity.HasOne(x => x.EvaluatorProfile)
                .WithMany(x => x.SyncLogs)
                .HasForeignKey(x => x.EvaluatorProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.EvaluationRound)
                .WithMany()
                .HasForeignKey(x => x.EvaluationRoundId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}