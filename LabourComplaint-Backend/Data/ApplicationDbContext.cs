// Data/ApplicationDbContext.cs
using LabourComplaint_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace LabourComplaint_Backend.Data;

// This factory is used by EF Core tools (like migrations) to create the DbContext at design time.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration the same way your app does
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()

            // Loads User Secrets in Development (for EF CLI tools)
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)

            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // PostgreSQL connection - User Secrets will override appsettings.json in Dev
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString) || connectionString.Contains("placeholder"))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Run 'dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"your-connection-string\"' for local development.");
        }

        optionsBuilder.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly("LabourComplaint-Backend"));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<District> Districts => Set<District>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Employer> Employers => Set<Employer>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintStatusHistory> ComplaintStatusHistories => Set<ComplaintStatusHistory>();
    public DbSet<EvidenceItem> EvidenceItems => Set<EvidenceItem>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === ENUM CONVERSIONS ===
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Complaint>().Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Complaint>().Property(c => c.Severity).HasConversion<string>().HasMaxLength(10);
        modelBuilder.Entity<Message>().Property(m => m.Direction).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<ChatRoom>().Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Notification>().Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<Notification>().Property(n => n.Channel).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Notification>().Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Notification>().Property(n => n.Priority).HasConversion<string>().HasMaxLength(10);
        modelBuilder.Entity<ComplaintStatusHistory>().Property(h => h.OldStatus).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<ComplaintStatusHistory>().Property(h => h.NewStatus).HasConversion<string>().HasMaxLength(20);

        // === GLOBAL QUERY FILTERS (Soft Delete) ===
        modelBuilder.Entity<District>().HasQueryFilter(d => !d.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Employer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Complaint>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<EvidenceItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ChatRoom>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Message>().HasQueryFilter(m => !m.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(n => !n.IsDeleted);
        modelBuilder.Entity<ComplaintStatusHistory>().HasQueryFilter(h => !h.IsDeleted);
        modelBuilder.Entity<UserDevice>().HasQueryFilter(d => !d.IsDeleted);
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasIndex(o => new { o.ProcessedAt, o.RetryCount });
            entity.HasIndex(o => new { o.DistrictId, o.EventType }); // Index for district filtering
            entity.Property(o => o.EventType).HasMaxLength(100);
        });

        // === RELATIONSHIPS (Normalized FKs, Restrict cascades to prevent cycles) ===
        modelBuilder.Entity<Complaint>()
            .HasOne(c => c.Reporter).WithMany(u => u.ReportedComplaints).HasForeignKey(c => c.ReporterId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Complaint>()
            .HasOne(c => c.AssignedInspector).WithMany(u => u.InspectedComplaints).HasForeignKey(c => c.AssignedInspectorId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Complaint>()
            .HasOne(c => c.District).WithMany(d => d.Complaints).HasForeignKey(c => c.DistrictId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComplaintStatusHistory>()
            .HasOne(h => h.Complaint).WithMany(c => c.StatusHistory).HasForeignKey(h => h.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ComplaintStatusHistory>()
            .HasOne(h => h.ChangedByUser).WithMany(u => u.StatusChanges).HasForeignKey(h => h.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatRoom>()
            .HasOne(cr => cr.Complaint).WithOne(c => c.ChatRoom).HasForeignKey<ChatRoom>(cr => cr.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ChatRoom>()
            .HasOne(cr => cr.Citizen).WithMany(u => u.InitiatedChats).HasForeignKey(cr => cr.CitizenId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChatRoom>()
            .HasOne(cr => cr.Inspector).WithMany(u => u.AssignedChats).HasForeignKey(cr => cr.InspectorId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.ChatRoom).WithMany(cr => cr.Messages).HasForeignKey(m => m.ChatRoomId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender).WithMany(u => u.SentMessages).HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Message>()
            .HasOne(m => m.ParentMessage).WithMany(m => m.Replies).HasForeignKey(m => m.ParentMessageId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Recipient).WithMany(u => u.ReceivedNotifications).HasForeignKey(n => n.RecipientId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.TriggeredByUser).WithMany().HasForeignKey(n => n.TriggeredByUserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Complaint).WithMany(c => c.RelatedNotifications).HasForeignKey(n => n.ComplaintId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<EvidenceItem>()
            .HasOne(e => e.Complaint).WithMany(c => c.Evidence).HasForeignKey(e => e.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<EvidenceItem>()
            .HasOne(e => e.UploadedByUser).WithMany(u => u.UploadedEvidence).HasForeignKey(e => e.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserDevice>()
            .HasOne(d => d.User).WithMany(u => u.Devices).HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);

        // === INDEXES (Optimized for Normalized Joins) ===
        modelBuilder.Entity<Complaint>()
            .HasIndex(c => new { c.DistrictId, c.Status, c.CreatedAt }).HasDatabaseName("IX_Complaint_District_Status_Created");
        modelBuilder.Entity<Complaint>()
            .HasIndex(c => c.ReferenceNumber).IsUnique();

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.RecipientId, n.Status, n.CreatedAt }).HasDatabaseName("IX_Notification_Recipient_Status_Created");
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.ComplaintId, n.Type, n.CreatedAt });
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.Status, n.Priority, n.CreatedAt }).HasFilter("\"Status\" = 'Pending'");

        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.ChatRoomId, m.CreatedAt });

        modelBuilder.Entity<ChatRoom>()
            .HasIndex(cr => new { cr.CitizenId, cr.Status, cr.CreatedAt });
        modelBuilder.Entity<ChatRoom>()
            .HasIndex(cr => new { cr.InspectorId, cr.Status, cr.CreatedAt });

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.AssignedDistrictId);
    }
}