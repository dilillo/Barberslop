using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Features.Invitation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Pages.Invitation;

[Authorize(Policy = "RequireBarber")]
public class ClientsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly InvitationService _invitationService;
    private readonly UserManager<IdentityUser> _userManager;

    public ClientsModel(
        ApplicationDbContext db,
        InvitationService invitationService,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _invitationService = invitationService;
        _userManager = userManager;
    }

    public Guid BarberProfileId { get; set; }
    public List<ClientSummary> ActiveClients { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        var barber = await _db.BarberProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
        if (barber == null) return;

        BarberProfileId = barber.Id;

        ActiveClients = await _db.InvitationRequests
            .Include(i => i.ClientProfile)
            .Where(i => i.BarberProfileId == barber.Id && i.Status == InvitationStatus.Active)
            .Select(i => new ClientSummary(
                i.Id,
                i.ClientProfile.DisplayName,
                i.ClientProfile.Email,
                i.ClientProfile.PhoneNumber,
                i.DecidedAt ?? i.RequestedAt))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDisinviteAsync(Guid invitationRequestId, string reason)
    {
        var userId = _userManager.GetUserId(User)!;
        var disinviteReason = Enum.TryParse<DisinviteReason>(reason, out var parsed)
            ? parsed
            : DisinviteReason.Other;

        await _invitationService.DisinviteClientAsync(invitationRequestId, userId, disinviteReason);
        return RedirectToPage();
    }
}

public record ClientSummary(Guid InvitationId, string DisplayName, string Email, string Phone, DateTimeOffset AcceptedAt);
