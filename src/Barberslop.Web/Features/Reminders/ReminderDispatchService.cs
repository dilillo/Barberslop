using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Features.Reminders;

public class ReminderDispatchService : IReminderDispatchService
{
    private readonly ApplicationDbContext _db;

    public ReminderDispatchService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task ScheduleRemindersAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await _db.Appointments
            .Include(a => a.ClientProfile)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null) return;

        var reminders = new List<ReminderDispatch>();

        // Immediate confirmation: Email + CalendarInvite
        reminders.Add(CreateDispatch(appointmentId, ReminderChannel.Email, DateTimeOffset.UtcNow));
        reminders.Add(CreateDispatch(appointmentId, ReminderChannel.CalendarInvite, DateTimeOffset.UtcNow));

        // 48 hours before: Email, SMS, Push
        var fortyEightHoursBefore = appointment.StartAt.AddHours(-48);
        if (fortyEightHoursBefore > DateTimeOffset.UtcNow)
        {
            reminders.Add(CreateDispatch(appointmentId, ReminderChannel.Email, fortyEightHoursBefore));
            reminders.Add(CreateDispatch(appointmentId, ReminderChannel.SMS, fortyEightHoursBefore));
            reminders.Add(CreateDispatch(appointmentId, ReminderChannel.Push, fortyEightHoursBefore));
        }

        // 24 hours before: Email, SMS, Push
        var twentyFourHoursBefore = appointment.StartAt.AddHours(-24);
        if (twentyFourHoursBefore > DateTimeOffset.UtcNow)
        {
            reminders.Add(CreateDispatch(appointmentId, ReminderChannel.Email, twentyFourHoursBefore));
            reminders.Add(CreateDispatch(appointmentId, ReminderChannel.SMS, twentyFourHoursBefore));
            reminders.Add(CreateDispatch(appointmentId, ReminderChannel.Push, twentyFourHoursBefore));
        }

        // 2 hours before: SMS, Push
        var twoHoursBefore = appointment.StartAt.AddHours(-2);
        if (twoHoursBefore > DateTimeOffset.UtcNow)
        {
            reminders.Add(CreateDispatch(appointmentId, ReminderChannel.SMS, twoHoursBefore));
            reminders.Add(CreateDispatch(appointmentId, ReminderChannel.Push, twoHoursBefore));
        }

        _db.ReminderDispatches.AddRange(reminders);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ReminderDispatch CreateDispatch(Guid appointmentId, ReminderChannel channel, DateTimeOffset scheduledFor)
    {
        return new ReminderDispatch
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Channel = channel,
            ScheduledFor = scheduledFor,
            Status = ReminderStatus.Pending
        };
    }
}
