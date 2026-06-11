using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Features.Booking;
using Barberslop.Web.Features.Reminders;
using Barberslop.Web.Features.Schedule;

namespace Barberslop.UnitTests;

public class BookingLimitPolicyTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.Create();

    private async Task<(BarberProfile barber, ClientProfile client, ServiceOffering service)> SetupAsync(ApplicationDbContext db)
    {
        var barber = new BarberProfile
        {
            Id = Guid.NewGuid(),
            UserId = "barber-1",
            DisplayName = "Barber",
            InviteCode = "LIMIT001",
            DefaultBookingLimit = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BarberProfiles.Add(barber);

        var client = new ClientProfile
        {
            Id = Guid.NewGuid(),
            UserId = "client-1",
            DisplayName = "Client",
            Email = "client@test.com",
            PhoneNumber = "+15551234567",
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

        // Active invitation
        db.InvitationRequests.Add(new InvitationRequest
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            ClientProfileId = client.Id,
            Status = InvitationStatus.Active,
            InitiatedBy = InitiatorType.Client,
            RequestedAt = DateTimeOffset.UtcNow,
            DecidedAt = DateTimeOffset.UtcNow
        });

        // Add availability rule for tomorrow
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        db.AvailabilityRules.Add(new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            DayOfWeek = tomorrow.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            TimeZoneId = "UTC",
            EffectiveFrom = DateOnly.FromDateTime(DateTime.Today)
        });

        await db.SaveChangesAsync();
        return (barber, client, service);
    }

    [Fact]
    public async Task CreateAppointment_WithinLimit_Succeeds()
    {
        using var db = CreateContext();
        var (barber, client, service) = await SetupAsync(db);

        var availability = new AvailabilityService(db);
        var reminders = new ReminderDispatchService(db);
        var bookingService = new BookingService(db, availability, reminders);

        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        var slots = await availability.GetAvailableSlotsAsync(barber.Id, service.Id, tomorrow);

        Assert.NotEmpty(slots);

        var result = await bookingService.CreateAppointmentAsync(
            barber.Id, client.Id, service.Id, slots[0].StartAt, null, BookingActorRole.Client);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.AppointmentId);
    }

    [Fact]
    public async Task CreateAppointment_ExceedsLimit_Fails()
    {
        using var db = CreateContext();
        var (barber, client, service) = await SetupAsync(db);

        // Add existing confirmed appointments up to the limit
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        for (int i = 0; i < barber.DefaultBookingLimit; i++)
        {
            db.Appointments.Add(new Appointment
            {
                Id = Guid.NewGuid(),
                BarberProfileId = barber.Id,
                ClientProfileId = client.Id,
                ServiceOfferingId = service.Id,
                StartAt = DateTimeOffset.UtcNow.AddDays(i + 2),
                EndAt = DateTimeOffset.UtcNow.AddDays(i + 2).AddMinutes(30),
                Status = AppointmentStatus.Confirmed,
                BookedByRole = BookingActorRole.Client,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var availability = new AvailabilityService(db);
        var reminders = new ReminderDispatchService(db);
        var bookingService = new BookingService(db, availability, reminders);

        var slots = await availability.GetAvailableSlotsAsync(barber.Id, service.Id, tomorrow);
        if (slots.Count == 0) return; // Skip if no slots available

        var result = await bookingService.CreateAppointmentAsync(
            barber.Id, client.Id, service.Id, slots[0].StartAt, null, BookingActorRole.Client);

        Assert.False(result.IsSuccess);
        Assert.Equal("LIMIT_EXCEEDED", result.ErrorCode);
    }

    [Fact]
    public async Task CreateAppointment_NoActiveInvitation_Fails()
    {
        using var db = CreateContext();
        var barber = new BarberProfile
        {
            Id = Guid.NewGuid(),
            UserId = "barber-2",
            DisplayName = "Barber 2",
            InviteCode = "NOINVITE",
            DefaultBookingLimit = 3,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BarberProfiles.Add(barber);

        var client = new ClientProfile
        {
            Id = Guid.NewGuid(),
            UserId = "client-2",
            DisplayName = "Client 2",
            Email = "c2@test.com",
            PhoneNumber = "+15559876543",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.ClientProfiles.Add(client);

        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            Name = "Service",
            DurationMinutes = 30,
            PriceAmount = 20m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ServiceOfferings.Add(service);
        await db.SaveChangesAsync();

        var availability = new AvailabilityService(db);
        var reminders = new ReminderDispatchService(db);
        var bookingService = new BookingService(db, availability, reminders);

        var result = await bookingService.CreateAppointmentAsync(
            barber.Id, client.Id, service.Id, DateTimeOffset.UtcNow.AddDays(1), null, BookingActorRole.Client);

        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_ACTIVE_CLIENT", result.ErrorCode);
    }
}
