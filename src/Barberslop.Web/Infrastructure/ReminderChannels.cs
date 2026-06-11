using Barberslop.Web.Domain;
using Microsoft.Extensions.Logging;

namespace Barberslop.Web.Infrastructure;

public class EmailReminderChannel : IReminderChannel
{
    private readonly ILogger<EmailReminderChannel> _logger;

    public EmailReminderChannel(ILogger<EmailReminderChannel> logger)
    {
        _logger = logger;
    }

    public ReminderChannel Channel => ReminderChannel.Email;

    public Task<ReminderResult> SendAsync(ReminderMessage message, CancellationToken cancellationToken = default)
    {
        // In production, this would use SendGrid API
        _logger.LogInformation(
            "Email reminder for appointment {AppointmentId} to {ClientEmail}: {ServiceName} with {BarberName} at {StartAt}",
            message.AppointmentId, message.ClientEmail, message.ServiceName, message.BarberName, message.AppointmentStartAt);

        return Task.FromResult(new ReminderResult(true, $"email-{Guid.NewGuid():N}"));
    }
}

public class SmsReminderChannel : IReminderChannel
{
    private readonly ILogger<SmsReminderChannel> _logger;

    public SmsReminderChannel(ILogger<SmsReminderChannel> logger)
    {
        _logger = logger;
    }

    public ReminderChannel Channel => ReminderChannel.SMS;

    public Task<ReminderResult> SendAsync(ReminderMessage message, CancellationToken cancellationToken = default)
    {
        // In production, this would use Twilio API
        _logger.LogInformation(
            "SMS reminder for appointment {AppointmentId} to {Phone}: {ServiceName} with {BarberName} at {StartAt}",
            message.AppointmentId, message.ClientPhoneNumber, message.ServiceName, message.BarberName, message.AppointmentStartAt);

        return Task.FromResult(new ReminderResult(true, $"sms-{Guid.NewGuid():N}"));
    }
}

public class CalendarInviteReminderChannel : IReminderChannel
{
    private readonly ILogger<CalendarInviteReminderChannel> _logger;

    public CalendarInviteReminderChannel(ILogger<CalendarInviteReminderChannel> logger)
    {
        _logger = logger;
    }

    public ReminderChannel Channel => ReminderChannel.CalendarInvite;

    public Task<ReminderResult> SendAsync(ReminderMessage message, CancellationToken cancellationToken = default)
    {
        // In production, this would generate an iCalendar .ics file and send via email
        _logger.LogInformation(
            "Calendar invite for appointment {AppointmentId} to {ClientEmail}",
            message.AppointmentId, message.ClientEmail);

        return Task.FromResult(new ReminderResult(true, $"cal-{Guid.NewGuid():N}"));
    }
}

public class PushNotificationReminderChannel : IReminderChannel
{
    private readonly ILogger<PushNotificationReminderChannel> _logger;

    public PushNotificationReminderChannel(ILogger<PushNotificationReminderChannel> logger)
    {
        _logger = logger;
    }

    public ReminderChannel Channel => ReminderChannel.Push;

    public Task<ReminderResult> SendAsync(ReminderMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(message.ClientPushToken))
        {
            return Task.FromResult(new ReminderResult(false, FailureReason: "Push not configured"));
        }

        _logger.LogInformation(
            "Push notification for appointment {AppointmentId} to token {Token}",
            message.AppointmentId, message.ClientPushToken);

        return Task.FromResult(new ReminderResult(false, FailureReason: "Push not configured"));
    }
}
