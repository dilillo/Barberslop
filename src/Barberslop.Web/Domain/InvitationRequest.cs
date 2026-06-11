namespace Barberslop.Web.Domain;

public class InvitationRequest
{
    public Guid Id { get; set; }
    public Guid BarberProfileId { get; set; }
    public Guid ClientProfileId { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public InitiatorType InitiatedBy { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public DateTimeOffset? DisinvitedAt { get; set; }
    public DisinviteReason? DisinviteReason { get; set; }
    public DateTimeOffset? NotificationSentAt { get; set; }

    public BarberProfile BarberProfile { get; set; } = null!;
    public ClientProfile ClientProfile { get; set; } = null!;
}
