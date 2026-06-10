using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Features.Reminders;
using Barberslop.Web.Features.Schedule;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Features.Booking;

public class BookingService
{
    private readonly ApplicationDbContext _db;
    private readonly IAvailabilityService _availability;
    private readonly IReminderDispatchService _reminders;

    public BookingService(
        ApplicationDbContext db,
        IAvailabilityService availability,
        IReminderDispatchService reminders)
    {
        _db = db;
        _availability = availability;
        _reminders = reminders;
    }

    public async Task<BookingResult> CreateAppointmentAsync(
        Guid barberProfileId,
        Guid clientProfileId,
        Guid serviceOfferingId,
        DateTimeOffset slotStartAt,
        Guid? familyMemberId,
        BookingActorRole bookedByRole,
        CancellationToken cancellationToken = default)
    {
        // Verify active invitation
        var hasActiveInvitation = await _db.InvitationRequests
            .AnyAsync(i => i.BarberProfileId == barberProfileId
                && i.ClientProfileId == clientProfileId
                && i.Status == InvitationStatus.Active, cancellationToken);

        if (!hasActiveInvitation)
            return BookingResult.Failure("NOT_ACTIVE_CLIENT");

        // Verify service
        var service = await _db.ServiceOfferings
            .FirstOrDefaultAsync(s => s.Id == serviceOfferingId
                && s.BarberProfileId == barberProfileId
                && s.IsActive, cancellationToken);

        if (service == null)
            return BookingResult.Failure("INVALID_SERVICE");

        // Verify family member ownership
        if (familyMemberId.HasValue)
        {
            var familyMember = await _db.FamilyMembers
                .AnyAsync(f => f.Id == familyMemberId.Value
                    && f.ClientProfileId == clientProfileId, cancellationToken);

            if (!familyMember)
                return BookingResult.Failure("INVALID_FAMILY_MEMBER");
        }

        // Check booking limit
        var barber = await _db.BarberProfiles
            .FirstOrDefaultAsync(b => b.Id == barberProfileId, cancellationToken);

        if (barber == null)
            return BookingResult.Failure("INVALID_BARBER");

        var futureBookingCount = await _db.Appointments
            .CountAsync(a => a.BarberProfileId == barberProfileId
                && a.ClientProfileId == clientProfileId
                && a.Status == AppointmentStatus.Confirmed
                && a.StartAt > DateTimeOffset.UtcNow, cancellationToken);

        if (futureBookingCount >= barber.DefaultBookingLimit)
            return BookingResult.Failure("LIMIT_EXCEEDED");

        // Verify slot availability
        var date = DateOnly.FromDateTime(slotStartAt.Date);
        var slots = await _availability.GetAvailableSlotsAsync(barberProfileId, serviceOfferingId, date, cancellationToken);
        var matchingSlot = slots.FirstOrDefault(s => s.StartAt == slotStartAt);

        if (matchingSlot == null)
            return BookingResult.Failure("SLOT_UNAVAILABLE");

        // Create appointment
        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barberProfileId,
            ClientProfileId = clientProfileId,
            FamilyMemberId = familyMemberId,
            ServiceOfferingId = serviceOfferingId,
            StartAt = matchingSlot.StartAt,
            EndAt = matchingSlot.EndAt,
            Status = AppointmentStatus.Confirmed,
            BookedByRole = bookedByRole,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync(cancellationToken);

        // Schedule reminders
        await _reminders.ScheduleRemindersAsync(appointment.Id, cancellationToken);

        return BookingResult.Ok(appointment.Id);
    }

    public async Task<string?> CancelAppointmentAsync(
        Guid appointmentId,
        string userId,
        string? cancellationReason,
        CancellationToken cancellationToken = default)
    {
        var appointment = await _db.Appointments
            .Include(a => a.BarberProfile)
            .Include(a => a.ClientProfile)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment == null)
            return "APPOINTMENT_NOT_FOUND";

        if (appointment.Status == AppointmentStatus.Cancelled)
            return "ALREADY_CANCELLED";

        if (appointment.BarberProfile.UserId != userId && appointment.ClientProfile.UserId != userId)
            return "UNAUTHORIZED";

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTimeOffset.UtcNow;
        appointment.CancellationReason = cancellationReason;

        await _db.SaveChangesAsync(cancellationToken);
        return null;
    }
}

public record BookingResult(bool IsSuccess, Guid? AppointmentId, string? ErrorCode)
{
    public static BookingResult Ok(Guid appointmentId) => new(true, appointmentId, null);
    public static BookingResult Failure(string errorCode) => new(false, null, errorCode);
}
