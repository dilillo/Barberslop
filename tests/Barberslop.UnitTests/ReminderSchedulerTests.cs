using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Features.Reminders;

namespace Barberslop.UnitTests;

public class ReminderSchedulerTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.Create();

    [Fact]
    public async Task ScheduleReminders_CreatesMultipleDispatches()
    {
        using var db = CreateContext();

        var barber = new BarberProfile
        {
            Id = Guid.NewGuid(),
            UserId = "barber-rem-1",
            DisplayName = "Barber",
            InviteCode = "REM00001",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BarberProfiles.Add(barber);

        var client = new ClientProfile
        {
            Id = Guid.NewGuid(),
            UserId = "client-rem-1",
            DisplayName = "Client",
            Email = "client@rem.com",
            PhoneNumber = "+15559998888",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.ClientProfiles.Add(client);

        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            Name = "Haircut",
            DurationMinutes = 30,
            PriceAmount = 25m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ServiceOfferings.Add(service);

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            ClientProfileId = client.Id,
            ServiceOfferingId = service.Id,
            StartAt = DateTimeOffset.UtcNow.AddDays(3),
            EndAt = DateTimeOffset.UtcNow.AddDays(3).AddMinutes(30),
            Status = AppointmentStatus.Confirmed,
            BookedByRole = BookingActorRole.Client,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var reminderService = new ReminderDispatchService(db);
        await reminderService.ScheduleRemindersAsync(appointment.Id);

        var dispatches = db.ReminderDispatches.Where(r => r.AppointmentId == appointment.Id).ToList();

        // Expect: 2 immediate (Email, CalendarInvite) + 3 at 48h + 3 at 24h + 2 at 2h = 10
        Assert.Equal(10, dispatches.Count);
        Assert.All(dispatches, d => Assert.Equal(ReminderStatus.Pending, d.Status));
    }

    [Fact]
    public async Task ScheduleReminders_ImmediateAppointment_SkipsFutureLeadTimes()
    {
        using var db = CreateContext();

        var barber = new BarberProfile
        {
            Id = Guid.NewGuid(),
            UserId = "barber-rem-2",
            DisplayName = "Barber",
            InviteCode = "REM00002",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BarberProfiles.Add(barber);

        var client = new ClientProfile
        {
            Id = Guid.NewGuid(),
            UserId = "client-rem-2",
            DisplayName = "Client",
            Email = "client2@rem.com",
            PhoneNumber = "+15557776666",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.ClientProfiles.Add(client);

        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            Name = "Quick Trim",
            DurationMinutes = 15,
            PriceAmount = 15m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ServiceOfferings.Add(service);

        // Appointment 1 hour from now - only 2h/24h/48h lead times are skipped
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            ClientProfileId = client.Id,
            ServiceOfferingId = service.Id,
            StartAt = DateTimeOffset.UtcNow.AddHours(1),
            EndAt = DateTimeOffset.UtcNow.AddHours(1).AddMinutes(15),
            Status = AppointmentStatus.Confirmed,
            BookedByRole = BookingActorRole.Client,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var reminderService = new ReminderDispatchService(db);
        await reminderService.ScheduleRemindersAsync(appointment.Id);

        var dispatches = db.ReminderDispatches.Where(r => r.AppointmentId == appointment.Id).ToList();

        // Only immediate dispatches (Email + CalendarInvite = 2)
        // 48h, 24h, 2h are all in the past relative to appointment start
        Assert.Equal(2, dispatches.Count);
        Assert.Contains(dispatches, d => d.Channel == ReminderChannel.Email);
        Assert.Contains(dispatches, d => d.Channel == ReminderChannel.CalendarInvite);
    }
}
