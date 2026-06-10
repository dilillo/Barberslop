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
public class PendingModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly InvitationService _invitationService;
    private readonly UserManager<IdentityUser> _userManager;

    public PendingModel(
        ApplicationDbContext db,
        InvitationService invitationService,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _invitationService = invitationService;
        _userManager = userManager;
    }

    public List<PendingRequestSummary> PendingRequests { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        var barber = await _db.BarberProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
        if (barber == null) return;

        PendingRequests = await _db.InvitationRequests
            .Include(i => i.ClientProfile)
            .Where(i => i.BarberProfileId == barber.Id && i.Status == InvitationStatus.Pending)
            .Select(i => new PendingRequestSummary(
                i.Id,
                i.ClientProfile.DisplayName,
                i.ClientProfile.Email,
                i.ClientProfile.PhoneNumber,
                i.RequestedAt))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAcceptAsync(Guid invitationRequestId)
    {
        var userId = _userManager.GetUserId(User)!;
        await _invitationService.AcceptInvitationAsync(invitationRequestId, userId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid invitationRequestId)
    {
        var userId = _userManager.GetUserId(User)!;
        await _invitationService.RejectInvitationAsync(invitationRequestId, userId);
        return RedirectToPage();
    }
}

public record PendingRequestSummary(Guid Id, string ClientName, string Email, string Phone, DateTimeOffset RequestedAt);
