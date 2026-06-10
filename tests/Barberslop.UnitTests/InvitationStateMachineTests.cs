using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Features.Invitation;

namespace Barberslop.UnitTests;

public class InvitationStateMachineTests
{
    private ApplicationDbContext CreateContext() => TestDbContextFactory.Create();

    private async Task<(BarberProfile barber, ClientProfile client)> SetupAsync(ApplicationDbContext db)
    {
        var barber = new BarberProfile
        {
            Id = Guid.NewGuid(),
            UserId = "barber-inv-1",
            DisplayName = "Barber",
            InviteCode = "INV00001",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.BarberProfiles.Add(barber);

        var client = new ClientProfile
        {
            Id = Guid.NewGuid(),
            UserId = "client-inv-1",
            DisplayName = "Client",
            Email = "client@inv.com",
            PhoneNumber = "+15551112222",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.ClientProfiles.Add(client);
        await db.SaveChangesAsync();

        return (barber, client);
    }

    [Fact]
    public async Task RequestInvitation_CreatesNewPendingInvitation()
    {
        using var db = CreateContext();
        var (barber, client) = await SetupAsync(db);
        var service = new InvitationService(db);

        var error = await service.RequestInvitationAsync(barber.Id, client.Id);

        Assert.Null(error);
        var invitation = db.InvitationRequests.Single();
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal(InitiatorType.Client, invitation.InitiatedBy);
    }

    [Fact]
    public async Task RequestInvitation_DuplicatePending_ReturnsError()
    {
        using var db = CreateContext();
        var (barber, client) = await SetupAsync(db);
        var service = new InvitationService(db);

        await service.RequestInvitationAsync(barber.Id, client.Id);
        var error = await service.RequestInvitationAsync(barber.Id, client.Id);

        Assert.Equal("ALREADY_PENDING", error);
    }

    [Fact]
    public async Task AcceptInvitation_TransitionsToActive()
    {
        using var db = CreateContext();
        var (barber, client) = await SetupAsync(db);
        var service = new InvitationService(db);

        await service.RequestInvitationAsync(barber.Id, client.Id);
        var invitation = db.InvitationRequests.Single();

        var error = await service.AcceptInvitationAsync(invitation.Id, barber.UserId);

        Assert.Null(error);
        await db.Entry(invitation).ReloadAsync();
        Assert.Equal(InvitationStatus.Active, invitation.Status);
        Assert.NotNull(invitation.DecidedAt);
    }

    [Fact]
    public async Task RejectInvitation_TransitionsToRejected()
    {
        using var db = CreateContext();
        var (barber, client) = await SetupAsync(db);
        var service = new InvitationService(db);

        await service.RequestInvitationAsync(barber.Id, client.Id);
        var invitation = db.InvitationRequests.Single();

        var error = await service.RejectInvitationAsync(invitation.Id, barber.UserId);

        Assert.Null(error);
        await db.Entry(invitation).ReloadAsync();
        Assert.Equal(InvitationStatus.Rejected, invitation.Status);
    }

    [Fact]
    public async Task DisinviteClient_TransitionsToDisinvited()
    {
        using var db = CreateContext();
        var (barber, client) = await SetupAsync(db);
        var service = new InvitationService(db);

        await service.RequestInvitationAsync(barber.Id, client.Id);
        var invitation = db.InvitationRequests.Single();
        await service.AcceptInvitationAsync(invitation.Id, barber.UserId);

        var error = await service.DisinviteClientAsync(invitation.Id, barber.UserId, DisinviteReason.RepeatedNoShow);

        Assert.Null(error);
        await db.Entry(invitation).ReloadAsync();
        Assert.Equal(InvitationStatus.Disinvited, invitation.Status);
        Assert.Equal(DisinviteReason.RepeatedNoShow, invitation.DisinviteReason);
        Assert.NotNull(invitation.DisinvitedAt);
    }

    [Fact]
    public async Task DisinvitedClient_CannotRequest_ReturnsError()
    {
        using var db = CreateContext();
        var (barber, client) = await SetupAsync(db);
        var service = new InvitationService(db);

        await service.RequestInvitationAsync(barber.Id, client.Id);
        var invitation = db.InvitationRequests.Single();
        await service.AcceptInvitationAsync(invitation.Id, barber.UserId);
        await service.DisinviteClientAsync(invitation.Id, barber.UserId, DisinviteReason.LostContact);

        var error = await service.RequestInvitationAsync(barber.Id, client.Id);

        Assert.Equal("DISINVITED", error);
    }

    [Fact]
    public async Task ReinviteClient_CreatesNewActiveInvitation()
    {
        using var db = CreateContext();
        var (barber, client) = await SetupAsync(db);
        var service = new InvitationService(db);

        await service.RequestInvitationAsync(barber.Id, client.Id);
        var invitation = db.InvitationRequests.Single();
        await service.AcceptInvitationAsync(invitation.Id, barber.UserId);
        await service.DisinviteClientAsync(invitation.Id, barber.UserId, DisinviteReason.Other);

        var error = await service.ReinviteClientAsync(client.Id, barber.UserId);

        Assert.Null(error);
        var newInvitation = db.InvitationRequests
            .Where(i => i.Status == InvitationStatus.Active)
            .OrderByDescending(i => i.RequestedAt)
            .First();
        Assert.Equal(InitiatorType.Barber, newInvitation.InitiatedBy);
    }

    [Fact]
    public async Task AcceptInvitation_WrongBarber_ReturnsUnauthorized()
    {
        using var db = CreateContext();
        var (barber, client) = await SetupAsync(db);
        var service = new InvitationService(db);

        await service.RequestInvitationAsync(barber.Id, client.Id);
        var invitation = db.InvitationRequests.Single();

        var error = await service.AcceptInvitationAsync(invitation.Id, "wrong-user-id");

        Assert.Equal("UNAUTHORIZED", error);
    }
}
