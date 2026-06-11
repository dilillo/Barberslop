namespace Barberslop.Web.Domain;

public class BarberProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? SalonName { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public double? GeoLatitude { get; set; }
    public double? GeoLongitude { get; set; }
    public int DefaultBookingLimit { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ServiceOffering> ServiceOfferings { get; set; } = new List<ServiceOffering>();
    public ICollection<AvailabilityRule> AvailabilityRules { get; set; } = new List<AvailabilityRule>();
    public ICollection<VacationPeriod> VacationPeriods { get; set; } = new List<VacationPeriod>();
    public ICollection<TemporaryUnavailability> TemporaryUnavailabilities { get; set; } = new List<TemporaryUnavailability>();
    public ICollection<InvitationRequest> InvitationRequests { get; set; } = new List<InvitationRequest>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
