using Microsoft.EntityFrameworkCore;
using SendellVoice.Domain.Entities;

namespace SendellVoice.Infrastructure.Data;

/// <summary>
/// Entity Framework Core database context for SendellVoice.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Intent> Intents => Set<Intent>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure JSON columns for collections and dictionaries
        ConfigureConversation(modelBuilder);
        ConfigureMessage(modelBuilder);
        ConfigureIntent(modelBuilder);
        ConfigureAgent(modelBuilder);
        ConfigureKnowledgeDocument(modelBuilder);
    }

    private static void ConfigureConversation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(50);
            entity.Property(e => e.CustomerEmail).HasMaxLength(256);
            entity.Property(e => e.Summary).HasMaxLength(2000);
            entity.Property(e => e.Resolution).HasMaxLength(2000);

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
                .HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.AssignedAgent)
                .WithMany(a => a.Conversations)
                .HasForeignKey(e => e.AssignedAgentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Intents)
                .WithOne(i => i.Conversation)
                .HasForeignKey(i => i.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private static void ConfigureMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.SenderName).HasMaxLength(200);
            entity.Property(e => e.SenderId).HasMaxLength(100);
            entity.Property(e => e.AudioUrl).HasMaxLength(2000);
            entity.Property(e => e.Transcription);
            entity.Property(e => e.Sentiment).HasMaxLength(50);

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private static void ConfigureIntent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Intent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reasoning).HasMaxLength(1000);
            entity.Property(e => e.OriginalText);
            entity.Property(e => e.SuggestedAction).HasMaxLength(500);

            entity.Property(e => e.ExtractedEntities)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.Message)
                .WithMany()
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.Category);
        });
    }

    private static void ConfigureAgent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Extension).HasMaxLength(20);
            entity.Property(e => e.Department).HasMaxLength(100);

            entity.Property(e => e.Skills)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsAvailable);
        });
    }

    private static void ConfigureKnowledgeDocument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.ProcessingError).HasMaxLength(2000);
            entity.Property(e => e.Version).HasMaxLength(50);

            entity.Property(e => e.Tags)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
                .HasColumnType("nvarchar(max)");

            entity.HasIndex(e => e.FileName);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);
        });
    }
}
