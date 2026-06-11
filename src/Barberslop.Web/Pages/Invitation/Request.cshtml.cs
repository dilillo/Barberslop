using Barberslop.Web.Data;
using Barberslop.Web.Features.Invitation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Pages.Invitation;

[Authorize(Policy = "RequireClient")]
public class RequestModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly InvitationService _invitationService;
    private readonly UserManager<IdentityUser> _userManager;

    public RequestModel(
        ApplicationDbContext db,
        InvitationService invitationService,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _invitationService = invitationService;
        _userManager = userManager;
    }

    [BindProperty]
    public Guid BarberProfileId { get; set; }

    public string BarberName { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User);
        var client = await _db.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == userId);

        if (client == null)
            return RedirectToPage("/Index");

        var error = await _invitationService.RequestInvitationAsync(BarberProfileId, client.Id);

        if (error != null)
        {
            TempData["Error"] = error;
            return RedirectToPage("/Invitation/Discover");
        }

        var barber = await _db.BarberProfiles.FirstOrDefaultAsync(b => b.Id == BarberProfileId);
        BarberName = barber?.DisplayName ?? "Unknown";

        return Page();
    }
}
