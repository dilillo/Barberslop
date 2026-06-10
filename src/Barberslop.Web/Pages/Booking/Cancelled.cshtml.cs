using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Features.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Pages.Booking;

[Authorize]
public class CancelledModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IAvailabilityService _availability;

    public CancelledModel(ApplicationDbContext db, IAvailabilityService availability)
    {
        _db = db;
        _availability = availability;
    }

    [BindProperty(SupportsGet = true)]
    public Guid AppointmentId { get; set; }

    public Guid BarberProfileId { get; set; }
    public Guid ServiceOfferingId { get; set; }
    public IReadOnlyList<TimeSlot> RebookingOptions { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == AppointmentId);

        if (appointment == null)
            return RedirectToPage("/Index");

        BarberProfileId = appointment.BarberProfileId;
        ServiceOfferingId = appointment.ServiceOfferingId;

        // Get next 3 available slots
        var today = DateOnly.FromDateTime(DateTime.Today);
        var allSlots = new List<TimeSlot>();
        for (int i = 0; i < 7 && allSlots.Count < 3; i++)
        {
            var slots = await _availability.GetAvailableSlotsAsync(
                BarberProfileId, ServiceOfferingId, today.AddDays(i));
            allSlots.AddRange(slots.Where(s => s.StartAt > DateTimeOffset.UtcNow));
        }

        RebookingOptions = allSlots.Take(3).ToList();
        return Page();
    }
}
