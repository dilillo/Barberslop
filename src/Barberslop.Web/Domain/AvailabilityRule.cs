namespace Barberslop.Web.Domain;

public class AvailabilityRule
{
    public Guid Id { get; set; }
    public Guid BarberProfileId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string TimeZoneId { get; set; } = "America/New_York";
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public BarberProfile BarberProfile { get; set; } = null!;
}
