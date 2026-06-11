namespace Barberslop.Web.Domain;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid BarberProfileId { get; set; }
    public Guid ClientProfileId { get; set; }
    public Guid? FamilyMemberId { get; set; }
    public Guid ServiceOfferingId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public bool IsStanding { get; set; }
    public string? StandingRecurrence { get; set; }
    public BookingActorRole BookedByRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public BarberProfile BarberProfile { get; set; } = null!;
    public ClientProfile ClientProfile { get; set; } = null!;
    public FamilyMember? FamilyMember { get; set; }
    public ServiceOffering ServiceOffering { get; set; } = null!;
    public ICollection<ReminderDispatch> ReminderDispatches { get; set; } = new List<ReminderDispatch>();
}
