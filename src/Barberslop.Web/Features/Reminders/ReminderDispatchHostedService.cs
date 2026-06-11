using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Barberslop.Web.Features.Reminders;

public class ReminderDispatchHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderDispatchHostedService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
    private static readonly int MaxRetries = 3;

    public ReminderDispatchHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReminderDispatchHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRemindersAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing reminders");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var channels = scope.ServiceProvider.GetRequiredService<IEnumerable<IReminderChannel>>();

        var pendingReminders = await db.ReminderDispatches
            .Include(r => r.Appointment)
                .ThenInclude(a => a.ClientProfile)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.BarberProfile)
            .Include(r => r.Appointment)
                .ThenInclude(a => a.ServiceOffering)
            .Where(r => r.Status == ReminderStatus.Pending
                && r.ScheduledFor <= DateTimeOffset.UtcNow
                && r.RetryCount < MaxRetries)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var reminder in pendingReminders)
        {
            var channel = channels.FirstOrDefault(c => c.Channel == reminder.Channel);
            if (channel == null)
            {
                reminder.Status = ReminderStatus.Failed;
                reminder.FailureReason = $"No channel implementation for {reminder.Channel}";
                reminder.AttemptedAt = DateTimeOffset.UtcNow;
                continue;
            }

            var message = new ReminderMessage(
                reminder.AppointmentId,
                reminder.Appointment.ClientProfile.DisplayName,
                reminder.Appointment.BarberProfile.DisplayName,
                reminder.Appointment.ServiceOffering.Name,
                reminder.Appointment.StartAt,
                reminder.Appointment.ClientProfile.Email,
                reminder.Appointment.ClientProfile.PhoneNumber,
                null);

            var result = await channel.SendAsync(message, cancellationToken);
            reminder.AttemptedAt = DateTimeOffset.UtcNow;

            if (result.Success)
            {
                reminder.Status = ReminderStatus.Sent;
                reminder.ExternalMessageId = result.ExternalMessageId;
                _logger.LogInformation("Reminder {ReminderId} sent via {Channel}", reminder.Id, reminder.Channel);
            }
            else
            {
                reminder.RetryCount++;
                if (reminder.RetryCount >= MaxRetries)
                {
                    reminder.Status = ReminderStatus.Failed;
                    _logger.LogWarning("Reminder {ReminderId} permanently failed: {Reason}", reminder.Id, result.FailureReason);
                }
                reminder.FailureReason = result.FailureReason;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
