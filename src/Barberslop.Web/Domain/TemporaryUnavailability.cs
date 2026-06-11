namespace Barberslop.Web.Domain;

public class TemporaryUnavailability
{
    public Guid Id { get; set; }
    public Guid BarberProfileId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string? Reason { get; set; }

    public BarberProfile BarberProfile { get; set; } = null!;
}
