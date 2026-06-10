namespace Barberslop.Web.Domain;

public class VacationPeriod
{
    public Guid Id { get; set; }
    public Guid BarberProfileId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }

    public BarberProfile BarberProfile { get; set; } = null!;
}
