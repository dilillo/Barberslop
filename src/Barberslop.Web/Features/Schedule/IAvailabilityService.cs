using Barberslop.Web.Domain;

namespace Barberslop.Web.Features.Schedule;

public interface IAvailabilityService
{
    Task<IReadOnlyList<TimeSlot>> GetAvailableSlotsAsync(
        Guid barberProfileId,
        Guid serviceOfferingId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<TimeSlot?> GetFirstAvailableSlotAsync(
        Guid barberProfileId,
        Guid serviceOfferingId,
        DateTimeOffset searchFrom,
        int searchHorizonDays = 90,
        CancellationToken cancellationToken = default);
}
