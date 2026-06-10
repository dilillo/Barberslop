using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Features.Schedule;

namespace Barberslop.UnitTests;

public class AvailabilityServiceTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.Create();

    private static BarberProfile CreateBarber(ApplicationDbContext db)
    {
        var barber = new BarberProfile
        {
            Id = Guid.NewGuid(),
            UserId = "barber-user-1",
            DisplayName = "Test Barber",
            InviteCode = "TESTCODE",
            DefaultBookingLimit = 3,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BarberProfiles.Add(barber);
        return barber;
    }

    private static ServiceOffering CreateService(ApplicationDbContext db, Guid barberId, int durationMinutes = 30)
    {
        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barberId,
            Name = "Haircut",
            DurationMinutes = durationMinutes,
            PriceAmount = 25.00m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ServiceOfferings.Add(service);
        return service;
    }

    [Fact]
    public async Task GetAvailableSlots_NoRules_ReturnsEmpty()
    {
        using var db = CreateContext();
        var barber = CreateBarber(db);
        var service = CreateService(db, barber.Id);
        await db.SaveChangesAsync();

        var svc = new AvailabilityService(db);
        var slots = await svc.GetAvailableSlotsAsync(barber.Id, service.Id, DateOnly.FromDateTime(DateTime.Today));

        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetAvailableSlots_WithRule_ReturnsSlots()
    {
        using var db = CreateContext();
        var barber = CreateBarber(db);
        var service = CreateService(db, barber.Id, 30);

        var today = DateOnly.FromDateTime(DateTime.Today);
        db.AvailabilityRules.Add(new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            DayOfWeek = today.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            TimeZoneId = "UTC",
            EffectiveFrom = today.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var svc = new AvailabilityService(db);
        var slots = await svc.GetAvailableSlotsAsync(barber.Id, service.Id, today);

        // 3 hours / 30 min = 6 slots
        Assert.Equal(6, slots.Count);
    }

    [Fact]
    public async Task GetAvailableSlots_WithVacation_ReturnsEmpty()
    {
        using var db = CreateContext();
        var barber = CreateBarber(db);
        var service = CreateService(db, barber.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);
        db.AvailabilityRules.Add(new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            DayOfWeek = today.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0),
            TimeZoneId = "UTC",
            EffectiveFrom = today.AddDays(-1)
        });
        db.VacationPeriods.Add(new VacationPeriod
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            StartDate = today,
            EndDate = today.AddDays(7)
        });
        await db.SaveChangesAsync();

        var svc = new AvailabilityService(db);
        var slots = await svc.GetAvailableSlotsAsync(barber.Id, service.Id, today);

        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetAvailableSlots_WithExistingAppointment_ExcludesOccupiedSlot()
    {
        using var db = CreateContext();
        var barber = CreateBarber(db);
        var service = CreateService(db, barber.Id, 60);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var tz = TimeZoneInfo.Utc;
        var ruleStart = new TimeOnly(9, 0);
        var ruleEnd = new TimeOnly(12, 0);

        db.AvailabilityRules.Add(new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            DayOfWeek = today.DayOfWeek,
            StartTime = ruleStart,
            EndTime = ruleEnd,
            TimeZoneId = "UTC",
            EffectiveFrom = today.AddDays(-1)
        });

        var startAt = new DateTimeOffset(today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        db.Appointments.Add(new Appointment
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            ClientProfileId = Guid.NewGuid(),
            ServiceOfferingId = service.Id,
            StartAt = startAt,
            EndAt = startAt.AddHours(1),
            Status = AppointmentStatus.Confirmed,
            BookedByRole = BookingActorRole.Client,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var svc = new AvailabilityService(db);
        var slots = await svc.GetAvailableSlotsAsync(barber.Id, service.Id, today);

        // 3 hour window - 1 hour occupied = 2 slots
        Assert.Equal(2, slots.Count);
        Assert.All(slots, s => Assert.True(s.StartAt >= startAt.AddHours(1)));
    }

    [Fact]
    public async Task GetAvailableSlots_InactiveService_ReturnsEmpty()
    {
        using var db = CreateContext();
        var barber = CreateBarber(db);
        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            Name = "Inactive Service",
            DurationMinutes = 30,
            PriceAmount = 20m,
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ServiceOfferings.Add(service);
        await db.SaveChangesAsync();

        var svc = new AvailabilityService(db);
        var slots = await svc.GetAvailableSlotsAsync(barber.Id, service.Id, DateOnly.FromDateTime(DateTime.Today));

        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetFirstAvailableSlot_FindsSlotInFuture()
    {
        using var db = CreateContext();
        var barber = CreateBarber(db);
        var service = CreateService(db, barber.Id, 30);

        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        db.AvailabilityRules.Add(new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            DayOfWeek = tomorrow.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            TimeZoneId = "UTC",
            EffectiveFrom = tomorrow.AddDays(-1)
        });
        await db.SaveChangesAsync();

        var svc = new AvailabilityService(db);
        var slot = await svc.GetFirstAvailableSlotAsync(barber.Id, service.Id, DateTimeOffset.UtcNow);

        Assert.NotNull(slot);
    }
}
