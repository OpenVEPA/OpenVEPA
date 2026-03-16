using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpenVEPA.Storage.Entities;

namespace OpenVEPA.Storage;

/// <summary>EF Core database context for OpenVEPA SQLite persistence.</summary>
public sealed class OpenVepaDbContext : DbContext
{
    public DbSet<SessionEntity> Sessions => Set<SessionEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<TaskItemEntity> TaskItems => Set<TaskItemEntity>();
    public DbSet<AuditLogEntity> AuditLogEntries => Set<AuditLogEntity>();
    public DbSet<LlmAuditLogEntity> LlmAuditLogEntries => Set<LlmAuditLogEntity>();
    public DbSet<UserPreferenceEntity> UserPreferenceEntries => Set<UserPreferenceEntity>();
    public DbSet<AccessTokenEntity> AccessTokens => Set<AccessTokenEntity>();

    public OpenVepaDbContext(DbContextOptions<OpenVepaDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Enable SQLite WAL mode and set busy timeout after connection opens.
        // This is handled via the connection interceptor pattern below.
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureSessions(modelBuilder);
        ConfigureMessages(modelBuilder);
        ConfigureTaskItems(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureLlmAuditLog(modelBuilder);
        ConfigureUserPreferences(modelBuilder);
        ConfigureAccessTokens(modelBuilder);
    }

    /// <summary>Applies WAL mode and busy timeout pragmas on an open SQLite connection.</summary>
    public static void ApplyPragmas(SqliteConnection connection)
    {
        if (connection == null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        cmd.ExecuteNonQuery();
    }

    private static void ConfigureSessions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SessionEntity>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Session)
                .HasForeignKey(m => m.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.SessionId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();

            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.Timestamp);
        });
    }

    private static void ConfigureTaskItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItemEntity>(entity =>
        {
            entity.ToTable("task_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Prompt).IsRequired();
            entity.Property(e => e.AgentName).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => e.Status);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.ToTable("audit_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Action).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SessionId).HasMaxLength(64);
            entity.Property(e => e.TaskId).HasMaxLength(64);
            entity.Property(e => e.Timestamp).IsRequired();

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.SessionId);
        });
    }

    private static void ConfigureLlmAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LlmAuditLogEntity>(entity =>
        {
            entity.ToTable("llm_audit_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Provider).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.SessionId).HasMaxLength(64);
            entity.Property(e => e.TaskId).HasMaxLength(64);
            entity.Property(e => e.SkillName).HasMaxLength(200);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.SessionId);
        });
    }

    private static void ConfigureUserPreferences(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreferenceEntity>(entity =>
        {
            entity.ToTable("user_preferences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(200);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.Category);
        });
    }

    private static void ConfigureAccessTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessTokenEntity>(entity =>
        {
            entity.ToTable("access_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.HashedToken).IsRequired();
            entity.Property(e => e.Salt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
