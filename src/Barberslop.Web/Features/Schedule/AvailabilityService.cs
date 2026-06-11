using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Features.Schedule;

public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _db;

    public AvailabilityService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TimeSlot>> GetAvailableSlotsAsync(
        Guid barberProfileId,
        Guid serviceOfferingId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var service = await _db.ServiceOfferings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceOfferingId && s.BarberProfileId == barberProfileId, cancellationToken);

        if (service == null || !service.IsActive)
            return [];

        var rules = await _db.AvailabilityRules
            .AsNoTracking()
            .Where(r => r.BarberProfileId == barberProfileId
                && r.DayOfWeek == date.DayOfWeek
                && r.EffectiveFrom <= date
                && (r.EffectiveTo == null || r.EffectiveTo >= date))
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
            return [];

        var timeZoneId = rules[0].TimeZoneId;
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var vacations = await _db.VacationPeriods
            .AsNoTracking()
            .Where(v => v.BarberProfileId == barberProfileId
                && v.StartDate <= date && v.EndDate >= date)
            .AnyAsync(cancellationToken);

        if (vacations)
            return [];

        var dateStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), tz.GetUtcOffset(date.ToDateTime(TimeOnly.MinValue)));
        var dateEnd = dateStart.AddDays(1);

        var tempBlocks = await _db.TemporaryUnavailabilities
            .AsNoTracking()
            .Where(t => t.BarberProfileId == barberProfileId
                && t.StartAt < dateEnd && t.EndAt > dateStart)
            .Select(t => new TimeSlot(t.StartAt, t.EndAt))
            .ToListAsync(cancellationToken);

        var appointments = await _db.Appointments
            .AsNoTracking()
            .Where(a => a.BarberProfileId == barberProfileId
                && a.StartAt < dateEnd && a.EndAt > dateStart
                && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Pending))
            .Select(a => new TimeSlot(a.StartAt, a.EndAt))
            .ToListAsync(cancellationToken);

        var blockedIntervals = tempBlocks.Concat(appointments).ToList();

        var slots = new List<TimeSlot>();
        var duration = TimeSpan.FromMinutes(service.DurationMinutes);

        foreach (var rule in rules)
        {
            var ruleStart = new DateTimeOffset(date.ToDateTime(rule.StartTime), tz.GetUtcOffset(date.ToDateTime(rule.StartTime)));
            var ruleEnd = new DateTimeOffset(date.ToDateTime(rule.EndTime), tz.GetUtcOffset(date.ToDateTime(rule.EndTime)));

            var freeWindows = SubtractIntervals(ruleStart, ruleEnd, blockedIntervals);

            foreach (var window in freeWindows)
            {
                var current = window.StartAt;
                while (current + duration <= window.EndAt)
                {
                    slots.Add(new TimeSlot(current, current + duration));
                    current = current + duration;
                }
            }
        }

        return slots.OrderBy(s => s.StartAt).ToList();
    }

    public async Task<TimeSlot?> GetFirstAvailableSlotAsync(
        Guid barberProfileId,
        Guid serviceOfferingId,
        DateTimeOffset searchFrom,
        int searchHorizonDays = 90,
        CancellationToken cancellationToken = default)
    {
        var startDate = DateOnly.FromDateTime(searchFrom.Date);

        for (int i = 0; i < searchHorizonDays; i++)
        {
            var date = startDate.AddDays(i);
            var slots = await GetAvailableSlotsAsync(barberProfileId, serviceOfferingId, date, cancellationToken);

            foreach (var slot in slots)
            {
                if (slot.StartAt >= searchFrom)
                    return slot;
            }
        }

        return null;
    }

    private static List<TimeSlot> SubtractIntervals(DateTimeOffset start, DateTimeOffset end, List<TimeSlot> blocks)
    {
        var result = new List<TimeSlot>();
        var sortedBlocks = blocks
            .Where(b => b.StartAt < end && b.EndAt > start)
            .OrderBy(b => b.StartAt)
            .ToList();

        var current = start;

        foreach (var block in sortedBlocks)
        {
            if (block.StartAt > current)
            {
                result.Add(new TimeSlot(current, block.StartAt < end ? block.StartAt : end));
            }
            if (block.EndAt > current)
            {
                current = block.EndAt;
            }
        }

        if (current < end)
        {
            result.Add(new TimeSlot(current, end));
        }

        return result;
    }
}
