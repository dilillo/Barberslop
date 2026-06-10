using Barberslop.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Pages.Booking;

[Authorize]
public class ConfirmModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public ConfirmModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public Guid AppointmentId { get; set; }

    public string BarberName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string BookedFor { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var appointment = await _db.Appointments
            .Include(a => a.BarberProfile)
            .Include(a => a.ServiceOffering)
            .Include(a => a.ClientProfile)
            .Include(a => a.FamilyMember)
            .FirstOrDefaultAsync(a => a.Id == AppointmentId);

        if (appointment == null)
            return RedirectToPage("/Index");

        BarberName = appointment.BarberProfile.DisplayName;
        ServiceName = appointment.ServiceOffering.Name;
        StartAt = appointment.StartAt;
        EndAt = appointment.EndAt;
        BookedFor = appointment.FamilyMember?.DisplayName ?? appointment.ClientProfile.DisplayName;

        return Page();
    }
}
