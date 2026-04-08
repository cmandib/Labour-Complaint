// Features/Complaints/Services/SLAMonitorService.cs
using LabourComplaint_Backend.Data;
//using LabourComplaint_Backend.Features.Notifications.Dtos;
using LabourComplaint_Backend.Features.Notifications.Services;
using LabourComplaint_Backend.Models;
using LabourComplaint_Backend.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LabourComplaint_Backend.Features.Complaints.Services;

public class SLAMonitorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SLAMonitorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Configurable

    public SLAMonitorService(IServiceProvider services, ILogger<SLAMonitorService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitor started (checking every {IntervalMinutes} min)", _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndEscalateOverdueComplaintsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLA Monitor encountered an error");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndEscalateOverdueComplaintsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var escalatedCount = 0;

        // 🔍 Find complaints overdue for FIRST RESPONSE (24h SLA after assignment)
        var overdueForResponse = await db.Complaints
            .Include(c => c.District)
            .Include(c => c.Reporter)
            .Where(c =>
                c.Status == ComplaintStatus.Assigned &&
                c.ResponseDueBy < now &&
                !c.IsDeleted)
            .ToListAsync(ct);

        foreach (var complaint in overdueForResponse)
        {
            var result = await EscalateForResponseOverdueAsync(db, notificationService, complaint, ct);
            if (result) escalatedCount++;
        }

        // 🔍 Find complaints overdue for FINAL RESOLUTION (7-day SLA)
        var overdueForResolution = await db.Complaints
            .Include(c => c.District)
            .Include(c => c.Reporter)
            .Where(c =>
                c.Status == ComplaintStatus.UnderReview ||c.Status == ComplaintStatus.EvidenceRequested &&
                c.ResolutionDueBy < now &&
                !c.IsDeleted)
            .ToListAsync(ct);

        foreach (var complaint in overdueForResolution)
        {
            var result = await EscalateForResolutionOverdueAsync(db, notificationService, complaint, ct);
            if (result) escalatedCount++;
        }

        if (escalatedCount > 0)
            _logger.LogInformation("SLA Monitor: Escalated {Count} overdue complaints", escalatedCount);
    }

    private async Task<bool> EscalateForResponseOverdueAsync(
        ApplicationDbContext db, INotificationService notificationService, Complaint complaint, CancellationToken ct)
    {
        // Only escalate if not already escalated
        if (complaint.Status is ComplaintStatus.Escalated or ComplaintStatus.Resolved or ComplaintStatus.Closed)
            return false;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Update status
            var oldStatus = complaint.Status;
            complaint.Status = ComplaintStatus.Escalated;
            complaint.UpdatedAt = DateTime.UtcNow;

            // Log history
            db.ComplaintStatusHistories.Add(new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id,
                OldStatus = oldStatus,
                NewStatus = ComplaintStatus.Escalated,
                ChangedByUserId = 0, // System-automated
                Reason = $"SLA breach: No response within 24h (due: {complaint.ResponseDueBy:u})",
                IsAutomated = true,
                ChangedAt = DateTime.UtcNow,
                NextDueDate = complaint.ResolutionDueBy // Keep resolution deadline
            });

            await db.SaveChangesAsync(ct);

            // Notify: assigned inspector (if any) + district admin fallback
            var recipients = new List<int>();
            if (complaint.AssignedInspectorId.HasValue)
                recipients.Add(complaint.AssignedInspectorId.Value);

            // Fallback: find an admin in the same district (or any admin)
            var districtAdmin = await db.Users
                .FirstOrDefaultAsync(u => u.Role == UserRole.Admin &&
                                         (u.AssignedDistrictId == complaint.DistrictId || !u.AssignedDistrictId.HasValue) &&
                                         !u.IsDeleted, ct);
            if (districtAdmin != null)
                recipients.Add(districtAdmin.Id);

            foreach (var recipientId in recipients.Distinct())
            {
                await notificationService.CreateAsync(new NotificationCreateRequest
                {
                    RecipientId = recipientId,
                    Type = NotificationType.EscalationRequired,
                    Title = "⚠️ SLA Breach: Complaint Overdue for Response",
                    Body = $"Complaint {complaint.ReferenceNumber} ({complaint.Category}) in {complaint.District?.Name} was not responded to within 24 hours. Immediate action required.",
                    Channel = NotificationChannel.InApp,
                    Priority = PriorityLevel.Urgent,
                    ComplaintId = complaint.Id,
                    TriggeredByUserId = null,
                    ActionUrl = $"/complaints/{complaint.ReferenceNumber}",
                    Payload = new
                    {
                        ComplaintReference = complaint.ReferenceNumber,
                        BreachType = "ResponseOverdue",
                        OriginalDueBy = complaint.ResponseDueBy,
                        EscalatedAt = DateTime.UtcNow,
                        DistrictId = complaint.DistrictId
                    },
                    IsSystemGenerated = true,
                    CorrelationId = Guid.NewGuid().ToString("N")
                }, ct);
            }

            await tx.CommitAsync(ct);
            _logger.LogWarning("SLA Escalation: {Ref} overdue for response", complaint.ReferenceNumber);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<bool> EscalateForResolutionOverdueAsync(
        ApplicationDbContext db, INotificationService notificationService, Complaint complaint, CancellationToken ct)
    {
        if (complaint.Status is ComplaintStatus.Escalated or ComplaintStatus.Resolved or ComplaintStatus.Closed)
            return false;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var oldStatus = complaint.Status;
            complaint.Status = ComplaintStatus.Escalated;
            complaint.UpdatedAt = DateTime.UtcNow;

            db.ComplaintStatusHistories.Add(new ComplaintStatusHistory
            {
                ComplaintId = complaint.Id,
                OldStatus = oldStatus,
                NewStatus = ComplaintStatus.Escalated,
                ChangedByUserId = 0,
                Reason = $"SLA breach: No resolution within 7 days (due: {complaint.ResolutionDueBy:u})",
                IsAutomated = true,
                ChangedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);

            // Notify senior admins + district supervisor
            var seniorAdmins = await db.Users
                .Where(u => u.Role == UserRole.Admin && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync(ct);

            foreach (var adminId in seniorAdmins)
            {
                await notificationService.CreateAsync(new NotificationCreateRequest
                {
                    RecipientId = adminId,
                    Type = NotificationType.EscalationRequired,
                    Title = "🚨 Critical: Complaint Resolution Overdue",
                    Body = $"Complaint {complaint.ReferenceNumber} ({complaint.Category}) has exceeded the 7-day resolution SLA. Escalated to leadership.",
                    Channel = NotificationChannel.InApp,
                    Priority = PriorityLevel.Urgent,
                    ComplaintId = complaint.Id,
                    TriggeredByUserId = 0,
                    ActionUrl = $"/complaints/{complaint.ReferenceNumber}",
                    Payload = new
                    {
                        ComplaintReference = complaint.ReferenceNumber,
                        BreachType = "ResolutionOverdue",
                        OriginalDueBy = complaint.ResolutionDueBy,
                        EscalatedAt = DateTime.UtcNow,
                        CurrentStatus = complaint.Status.ToString(),
                        DistrictId = complaint.DistrictId
                    },
                    IsSystemGenerated = true,
                    CorrelationId = Guid.NewGuid().ToString("N")
                }, ct);
            }

            await tx.CommitAsync(ct);
            _logger.LogError("SLA Critical Escalation: {Ref} overdue for resolution", complaint.ReferenceNumber);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}