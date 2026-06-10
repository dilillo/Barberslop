namespace Barberslop.Web.Domain;

public class ServiceOffering
{
    public Guid Id { get; set; }
    public Guid BarberProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public BarberProfile BarberProfile { get; set; } = null!;
}
