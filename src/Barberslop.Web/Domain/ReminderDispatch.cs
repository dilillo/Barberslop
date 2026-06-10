namespace Barberslop.Web.Domain;

public class ReminderDispatch
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public ReminderChannel Channel { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;
    public DateTimeOffset? AttemptedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? ExternalMessageId { get; set; }
    public int RetryCount { get; set; }

    public Appointment Appointment { get; set; } = null!;
}
