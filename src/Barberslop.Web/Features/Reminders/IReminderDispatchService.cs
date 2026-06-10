namespace Barberslop.Web.Features.Reminders;

public interface IReminderDispatchService
{
    Task ScheduleRemindersAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);
}
