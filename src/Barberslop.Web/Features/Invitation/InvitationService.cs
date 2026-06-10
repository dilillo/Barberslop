using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Features.Invitation;

public class InvitationService
{
    private readonly ApplicationDbContext _db;

    public InvitationService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<string?> RequestInvitationAsync(
        Guid barberProfileId,
        Guid clientProfileId,
        CancellationToken cancellationToken = default)
    {
        var barber = await _db.BarberProfiles
            .AnyAsync(b => b.Id == barberProfileId, cancellationToken);

        if (!barber)
            return "INVALID_BARBER";

        var existing = await _db.InvitationRequests
            .FirstOrDefaultAsync(i => i.BarberProfileId == barberProfileId
                && i.ClientProfileId == clientProfileId
                && i.Status != InvitationStatus.Rejected, cancellationToken);

        if (existing != null)
        {
            if (existing.Status == InvitationStatus.Disinvited)
                return "DISINVITED";
            return "ALREADY_PENDING";
        }

        var invitation = new InvitationRequest
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barberProfileId,
            ClientProfileId = clientProfileId,
            Status = InvitationStatus.Pending,
            InitiatedBy = InitiatorType.Client,
            RequestedAt = DateTimeOffset.UtcNow
        };

        _db.InvitationRequests.Add(invitation);
        await _db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> AcceptInvitationAsync(
        Guid invitationRequestId,
        string barberUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _db.InvitationRequests
            .Include(i => i.BarberProfile)
            .FirstOrDefaultAsync(i => i.Id == invitationRequestId, cancellationToken);

        if (invitation == null)
            return "NOT_FOUND";

        if (invitation.BarberProfile.UserId != barberUserId)
            return "UNAUTHORIZED";

        if (invitation.Status != InvitationStatus.Pending)
            return "NOT_PENDING";

        invitation.Status = InvitationStatus.Active;
        invitation.DecidedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> RejectInvitationAsync(
        Guid invitationRequestId,
        string barberUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _db.InvitationRequests
            .Include(i => i.BarberProfile)
            .FirstOrDefaultAsync(i => i.Id == invitationRequestId, cancellationToken);

        if (invitation == null)
            return "NOT_FOUND";

        if (invitation.BarberProfile.UserId != barberUserId)
            return "UNAUTHORIZED";

        if (invitation.Status != InvitationStatus.Pending)
            return "NOT_PENDING";

        invitation.Status = InvitationStatus.Rejected;
        invitation.DecidedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> DisinviteClientAsync(
        Guid invitationRequestId,
        string barberUserId,
        DisinviteReason reason,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _db.InvitationRequests
            .Include(i => i.BarberProfile)
            .FirstOrDefaultAsync(i => i.Id == invitationRequestId, cancellationToken);

        if (invitation == null)
            return "NOT_FOUND";

        if (invitation.BarberProfile.UserId != barberUserId)
            return "UNAUTHORIZED";

        if (invitation.Status != InvitationStatus.Active)
            return "NOT_ACTIVE";

        invitation.Status = InvitationStatus.Disinvited;
        invitation.DisinvitedAt = DateTimeOffset.UtcNow;
        invitation.DisinviteReason = reason;
        await _db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<string?> ReinviteClientAsync(
        Guid clientProfileId,
        string barberUserId,
        CancellationToken cancellationToken = default)
    {
        var barber = await _db.BarberProfiles
            .FirstOrDefaultAsync(b => b.UserId == barberUserId, cancellationToken);

        if (barber == null)
            return "UNAUTHORIZED";

        var disinvited = await _db.InvitationRequests
            .FirstOrDefaultAsync(i => i.BarberProfileId == barber.Id
                && i.ClientProfileId == clientProfileId
                && i.Status == InvitationStatus.Disinvited, cancellationToken);

        if (disinvited == null)
            return "NOT_DISINVITED";

        var newInvitation = new InvitationRequest
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            ClientProfileId = clientProfileId,
            Status = InvitationStatus.Active,
            InitiatedBy = InitiatorType.Barber,
            RequestedAt = DateTimeOffset.UtcNow,
            DecidedAt = DateTimeOffset.UtcNow
        };

        _db.InvitationRequests.Add(newInvitation);
        await _db.SaveChangesAsync(cancellationToken);
        return null;
    }
}
