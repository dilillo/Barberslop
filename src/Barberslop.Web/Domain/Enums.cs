namespace Barberslop.Web.Domain;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Active,
    Rejected,
    Disinvited
}

public enum InitiatorType
{
    Client,
    Barber
}

public enum DisinviteReason
{
    RepeatedNoShow,
    LostContact,
    Other
}

public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled
}

public enum BookingActorRole
{
    Client,
    Barber
}

public enum ReminderChannel
{
    Email,
    SMS,
    CalendarInvite,
    Push
}

public enum ReminderStatus
{
    Pending,
    Sent,
    Failed
}
