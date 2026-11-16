using ClubPlatform.Api.Models;
using ClubPlatform.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;

namespace ClubPlatform.Api.Data;

/// <summary>
/// Главный контекст Entity Framework Core.
/// Через него API будет работать с PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Interest> Interests => Set<Interest>();

    public DbSet<Club> Clubs => Set<Club>();

    public DbSet<ClubInterest> ClubInterests => Set<ClubInterest>();

    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();

    public DbSet<ClubBan> ClubBans => Set<ClubBan>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<ClubSuggestion> ClubSuggestions =>
        Set<ClubSuggestion>();

    // Обращения пользователей в службу поддержки.
    public DbSet<SupportTicket> SupportTickets =>
        Set<SupportTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Используем PostgreSQL-схему public.
        modelBuilder.HasDefaultSchema("public");

        // -----------------------------------------------------
        // PostgreSQL ENUM
        // -----------------------------------------------------

        // В нашей БД значения enum имеют вид:
        // User, Admin
        // Male, Female, Other
        // Pending, Resolved, Rejected
        //
        // Npgsql по умолчанию может переводить имена
        // в snake_case. Нам это здесь не нужно,
        // поэтому используем NpgsqlNullNameTranslator.
        modelBuilder.HasPostgresEnum<UserRole>(
            name: "user_role",
            nameTranslator: new NpgsqlNullNameTranslator());

        modelBuilder.HasPostgresEnum<Gender>(
            name: "gender",
            nameTranslator: new NpgsqlNullNameTranslator());

        modelBuilder.HasPostgresEnum<ReportStatus>(
            name: "report_status",
            nameTranslator: new NpgsqlNullNameTranslator());

        // =====================================================
        // USERS
        // =====================================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(320)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .HasColumnName("password_hash")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.Role)
                .HasColumnName("role")
                .HasColumnType("user_role")
                .HasDefaultValue(UserRole.User)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Age)
                .HasColumnName("age")
                .IsRequired();

            entity.Property(x => x.Gender)
                .HasColumnName("gender")
                .HasColumnType("gender")
                .IsRequired();

            entity.Property(x => x.PhotoUrl)
                .HasColumnName("photo_url")
                .HasColumnType("text");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(x => x.Email)
                .HasDatabaseName("ux_users_email_lower");
        });

        // =====================================================
        // INTERESTS
        // =====================================================

        modelBuilder.Entity<Interest>(entity =>
        {
            entity.ToTable("interests");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Emoji)
                .HasColumnName("emoji")
                .HasMaxLength(20);

            entity.HasIndex(x => x.Name)
                .IsUnique()
                .HasDatabaseName("ux_interests_name");
        });

        // =====================================================
        // CLUBS
        // =====================================================

        modelBuilder.Entity<Club>(entity =>
        {
            entity.ToTable("clubs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.PhotoUrl)
                .HasColumnName("photo_url")
                .HasColumnType("text");

            entity.Property(x => x.Address)
                .HasColumnName("address")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.ContactPhone)
                .HasColumnName("contact_phone")
                .HasMaxLength(30);

            entity.Property(x => x.ContactEmail)
                .HasColumnName("contact_email")
                .HasMaxLength(320);

            entity.Property(x => x.MaxMembers)
                .HasColumnName("max_members");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });

        // =====================================================
        // CLUB_INTERESTS
        // =====================================================

        modelBuilder.Entity<ClubInterest>(entity =>
        {
            entity.ToTable("club_interests");

            // Составной первичный ключ.
            entity.HasKey(x => new
            {
                x.ClubId,
                x.InterestId
            });

            entity.Property(x => x.ClubId)
                .HasColumnName("club_id");

            entity.Property(x => x.InterestId)
                .HasColumnName("interest_id");

            entity.HasOne(x => x.Club)
                .WithMany(x => x.ClubInterests)
                .HasForeignKey(x => x.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Interest)
                .WithMany(x => x.ClubInterests)
                .HasForeignKey(x => x.InterestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.InterestId)
                .HasDatabaseName("ix_club_interests_interest_id");
        });

        // =====================================================
        // CLUB_MEMBERS
        // =====================================================

        modelBuilder.Entity<ClubMember>(entity =>
        {
            entity.ToTable("club_members");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.ClubId)
                .HasColumnName("club_id")
                .IsRequired();

            entity.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(x => x.JoinedAt)
                .HasColumnName("joined_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(x => x.IsMuted)
                .HasColumnName("is_muted")
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.ClubId,
                x.UserId
            })
                .IsUnique()
                .HasDatabaseName("ux_club_members_club_user");

            entity.HasIndex(x => x.UserId)
                .HasDatabaseName("ix_club_members_user_id");

            entity.HasIndex(x => x.ClubId)
                .HasDatabaseName("ix_club_members_club_id");

            entity.HasOne(x => x.Club)
                .WithMany(x => x.ClubMembers)
                .HasForeignKey(x => x.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.ClubMembers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // MESSAGES
        // =====================================================

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.ClubId)
                .HasColumnName("club_id")
                .IsRequired();

            entity.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(x => x.Content)
                .HasColumnName("content")
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(x => x.IsDeleted)
                .HasColumnName("is_deleted")
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.ClubId,
                x.CreatedAt
            })
                .HasDatabaseName("ix_messages_club_created_at");

            entity.HasIndex(x => x.UserId)
                .HasDatabaseName("ix_messages_user_id");

            entity.HasOne(x => x.Club)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // REPORTS
        // =====================================================

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("reports");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.MessageId)
                .HasColumnName("message_id")
                .IsRequired();

            entity.Property(x => x.ReporterUserId)
                .HasColumnName("reporter_user_id")
                .IsRequired();

            entity.Property(x => x.Reason)
                .HasColumnName("reason")
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasColumnType("report_status")
                .HasDefaultValue(ReportStatus.Pending)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.HasIndex(x => x.Status)
                .HasDatabaseName("ix_reports_status");

            entity.HasIndex(x => x.MessageId)
                .HasDatabaseName("ix_reports_message_id");

            entity.HasIndex(x => x.ReporterUserId)
                .HasDatabaseName("ix_reports_reporter_user_id");

            entity.HasOne(x => x.Message)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ReporterUser)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.ReporterUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // =====================================================
        // CLUB SUGGESTIONS
        // =====================================================

        modelBuilder.Entity<ClubSuggestion>(entity =>
        {
            entity.ToTable("club_suggestions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text")
                .IsRequired();

            entity.Property(x => x.InterestName)
                .HasColumnName("interest_name")
                .HasMaxLength(100);

            entity.Property(x => x.SuggestedAddress)
                .HasColumnName("suggested_address")
                .HasMaxLength(500);

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue("Pending")
                .IsRequired();

            entity.Property(x => x.AdminComment)
                .HasColumnName("admin_comment")
                .HasColumnType("text");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(x => x.ReviewedAt)
                .HasColumnName("reviewed_at")
                .HasColumnType("timestamp with time zone");

            entity.Property(x => x.CreatedClubId)
                .HasColumnName("created_club_id");

            entity.HasOne(x => x.CreatedClub)
                .WithMany()
                .HasForeignKey(x => x.CreatedClubId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.UserId)
                .HasDatabaseName("ix_club_suggestions_user_id");

            entity.HasIndex(x => x.Status)
                .HasDatabaseName("ix_club_suggestions_status");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClubBan>(entity =>
        {
            entity.ToTable("club_bans");

            entity.HasKey(item => item.Id);

            entity.Property(item => item.Id)
                .HasColumnName("id");

            entity.Property(item => item.ClubId)
                .HasColumnName("club_id");

            entity.Property(item => item.UserId)
                .HasColumnName("user_id");

            entity.Property(item => item.BannedByUserId)
                .HasColumnName("banned_by_user_id");

            entity.Property(item => item.Reason)
                .HasColumnName("reason")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(item => item.CreatedAt)
                .HasColumnName("created_at");

            entity.HasOne(item => item.Club)
                .WithMany()
                .HasForeignKey(item => item.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(item => new
            {
                item.ClubId,
                item.UserId
            }).IsUnique();
        });


        // =====================================================
        // SUPPORT TICKETS
        // =====================================================

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.ToTable("support_tickets");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(x => x.Subject)
                .HasColumnName("subject")
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Message)
                .HasColumnName("message")
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasDefaultValue("Pending")
                .IsRequired();

            entity.Property(x => x.AdminResponse)
                .HasColumnName("admin_response")
                .HasColumnType("text");

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(x => x.AnsweredAt)
                .HasColumnName("answered_at")
                .HasColumnType("timestamp with time zone");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserId)
                .HasDatabaseName("ix_support_tickets_user_id");

            entity.HasIndex(x => x.Status)
                .HasDatabaseName("ix_support_tickets_status");
        });


    }
}