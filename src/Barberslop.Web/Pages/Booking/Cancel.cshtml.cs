using Barberslop.Web.Features.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Barberslop.Web.Pages.Booking;

[Authorize]
public class CancelModel : PageModel
{
    private readonly BookingService _bookingService;
    private readonly UserManager<IdentityUser> _userManager;

    public CancelModel(BookingService bookingService, UserManager<IdentityUser> userManager)
    {
        _bookingService = bookingService;
        _userManager = userManager;
    }

    [BindProperty]
    public Guid AppointmentId { get; set; }

    [BindProperty]
    public string? CancellationReason { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        var error = await _bookingService.CancelAppointmentAsync(AppointmentId, userId, CancellationReason);

        if (error != null)
        {
            ErrorMessage = error;
            return Page();
        }

        return RedirectToPage("/Booking/Cancelled", new { appointmentId = AppointmentId });
    }
}
