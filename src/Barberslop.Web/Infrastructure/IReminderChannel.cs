using Barberslop.Web.Domain;

namespace Barberslop.Web.Infrastructure;

public interface IReminderChannel
{
    ReminderChannel Channel { get; }

    Task<ReminderResult> SendAsync(
        ReminderMessage message,
        CancellationToken cancellationToken = default);
}

public record ReminderMessage(
    Guid AppointmentId,
    string ClientName,
    string BarberName,
    string ServiceName,
    DateTimeOffset AppointmentStartAt,
    string ClientEmail,
    string ClientPhoneNumber,
    string? ClientPushToken);

public record ReminderResult(
    bool Success,
    string? ExternalMessageId = null,
    string? FailureReason = null);
