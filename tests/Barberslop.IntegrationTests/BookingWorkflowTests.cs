using System.Net;
using System.Net.Http.Headers;
using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Barberslop.IntegrationTests;

public class BookingWorkflowTests : IClassFixture<BarberWebApplicationFactory>
{
    private readonly BarberWebApplicationFactory _factory;

    public BookingWorkflowTests(BarberWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullBookingFlow_RegisterBarberAndClient_BookAppointment()
    {
        // This test verifies the database layer works for the full booking workflow
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Create barber user
        var barberUser = new IdentityUser { UserName = "barber@test.com", Email = "barber@test.com" };
        await userManager.CreateAsync(barberUser, "Password1!");
        await userManager.AddToRoleAsync(barberUser, "Barber");

        var barber = new BarberProfile
        {
            Id = Guid.NewGuid(),
            UserId = barberUser.Id,
            DisplayName = "Test Barber",
            InviteCode = "TEST0001",
            DefaultBookingLimit = 3,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BarberProfiles.Add(barber);

        // Create client user
        var clientUser = new IdentityUser { UserName = "client@test.com", Email = "client@test.com" };
        await userManager.CreateAsync(clientUser, "Password1!");
        await userManager.AddToRoleAsync(clientUser, "Client");

        var client = new ClientProfile
        {
            Id = Guid.NewGuid(),
            UserId = clientUser.Id,
            DisplayName = "Test Client",
            Email = "client@test.com",
            PhoneNumber = "+15551234567",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.ClientProfiles.Add(client);

        // Create service
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

        // Create active invitation
        var invitation = new InvitationRequest
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            ClientProfileId = client.Id,
            Status = InvitationStatus.Active,
            InitiatedBy = InitiatorType.Client,
            RequestedAt = DateTimeOffset.UtcNow,
            DecidedAt = DateTimeOffset.UtcNow
        };
        db.InvitationRequests.Add(invitation);

        // Create availability
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

        // Verify: Appointment can be created
        var bookingService = scope.ServiceProvider.GetRequiredService<Web.Features.Booking.BookingService>();
        var availabilityService = scope.ServiceProvider.GetRequiredService<Web.Features.Schedule.IAvailabilityService>();

        var slots = await availabilityService.GetAvailableSlotsAsync(barber.Id, service.Id, tomorrow);
        Assert.NotEmpty(slots);

        var result = await bookingService.CreateAppointmentAsync(
            barber.Id, client.Id, service.Id, slots[0].StartAt, null, BookingActorRole.Client);

        Assert.True(result.IsSuccess);

        // Verify appointment exists in DB
        var appointment = db.Appointments.FirstOrDefault(a => a.Id == result.AppointmentId);
        Assert.NotNull(appointment);
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);

        // Verify reminders were scheduled
        var reminders = db.ReminderDispatches.Where(r => r.AppointmentId == appointment.Id).ToList();
        Assert.NotEmpty(reminders);
    }

    [Fact]
    public async Task DoubleBookingPrevention_SecondBookingSameSlot_Fails()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var barberUser = new IdentityUser { UserName = "barber2@test.com", Email = "barber2@test.com" };
        await userManager.CreateAsync(barberUser, "Password1!");
        await userManager.AddToRoleAsync(barberUser, "Barber");

        var barber = new BarberProfile
        {
            Id = Guid.NewGuid(),
            UserId = barberUser.Id,
            DisplayName = "Barber 2",
            InviteCode = "TEST0002",
            DefaultBookingLimit = 5,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BarberProfiles.Add(barber);

        var client1User = new IdentityUser { UserName = "client1b@test.com", Email = "client1b@test.com" };
        await userManager.CreateAsync(client1User, "Password1!");
        var client1 = new ClientProfile
        {
            Id = Guid.NewGuid(), UserId = client1User.Id, DisplayName = "Client1",
            Email = "c1b@test.com", PhoneNumber = "+15551111111",
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.ClientProfiles.Add(client1);

        var client2User = new IdentityUser { UserName = "client2b@test.com", Email = "client2b@test.com" };
        await userManager.CreateAsync(client2User, "Password1!");
        var client2 = new ClientProfile
        {
            Id = Guid.NewGuid(), UserId = client2User.Id, DisplayName = "Client2",
            Email = "c2b@test.com", PhoneNumber = "+15552222222",
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.ClientProfiles.Add(client2);

        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(), BarberProfileId = barber.Id, Name = "Service",
            DurationMinutes = 60, PriceAmount = 30m, IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.ServiceOfferings.Add(service);

        // Active invitations for both clients
        db.InvitationRequests.Add(new InvitationRequest
        {
            Id = Guid.NewGuid(), BarberProfileId = barber.Id, ClientProfileId = client1.Id,
            Status = InvitationStatus.Active, InitiatedBy = InitiatorType.Client,
            RequestedAt = DateTimeOffset.UtcNow, DecidedAt = DateTimeOffset.UtcNow
        });
        db.InvitationRequests.Add(new InvitationRequest
        {
            Id = Guid.NewGuid(), BarberProfileId = barber.Id, ClientProfileId = client2.Id,
            Status = InvitationStatus.Active, InitiatedBy = InitiatorType.Client,
            RequestedAt = DateTimeOffset.UtcNow, DecidedAt = DateTimeOffset.UtcNow
        });

        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        db.AvailabilityRules.Add(new AvailabilityRule
        {
            Id = Guid.NewGuid(), BarberProfileId = barber.Id, DayOfWeek = tomorrow.DayOfWeek,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0),
            TimeZoneId = "UTC", EffectiveFrom = DateOnly.FromDateTime(DateTime.Today)
        });

        await db.SaveChangesAsync();

        var bookingService = scope.ServiceProvider.GetRequiredService<Web.Features.Booking.BookingService>();
        var availabilityService = scope.ServiceProvider.GetRequiredService<Web.Features.Schedule.IAvailabilityService>();

        var slots = await availabilityService.GetAvailableSlotsAsync(barber.Id, service.Id, tomorrow);
        Assert.Single(slots); // Only one 60-min slot in a 1-hour window

        // First booking succeeds
        var result1 = await bookingService.CreateAppointmentAsync(
            barber.Id, client1.Id, service.Id, slots[0].StartAt, null, BookingActorRole.Client);
        Assert.True(result1.IsSuccess);

        // Second booking at same slot fails
        var result2 = await bookingService.CreateAppointmentAsync(
            barber.Id, client2.Id, service.Id, slots[0].StartAt, null, BookingActorRole.Client);
        Assert.False(result2.IsSuccess);
        Assert.Equal("SLOT_UNAVAILABLE", result2.ErrorCode);
    }
}
