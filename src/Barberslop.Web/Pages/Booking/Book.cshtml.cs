using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Barberslop.Web.Features.Booking;
using Barberslop.Web.Features.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Pages.Booking;

[Authorize]
public class BookModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IAvailabilityService _availability;
    private readonly BookingService _bookingService;
    private readonly UserManager<IdentityUser> _userManager;

    public BookModel(
        ApplicationDbContext db,
        IAvailabilityService availability,
        BookingService bookingService,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _availability = availability;
        _bookingService = bookingService;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public Guid BarberProfileId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? ServiceOfferingId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    [BindProperty]
    public string? SlotStartAt { get; set; }

    [BindProperty]
    public Guid BookForId { get; set; }

    public string BarberName { get; set; } = string.Empty;
    public List<ServiceOption> Services { get; set; } = new();
    public IReadOnlyList<TimeSlot> AvailableSlots { get; set; } = [];
    public List<FamilyMemberOption> FamilyMembers { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = _userManager.GetUserId(User);
        var client = await _db.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == userId);

        if (client == null)
        {
            ValidationErrors.Add("Client profile not found.");
            await LoadPageDataAsync();
            return Page();
        }

        if (string.IsNullOrEmpty(SlotStartAt) || !DateTimeOffset.TryParse(SlotStartAt, out var slotStart))
        {
            ValidationErrors.Add("Please select a time slot.");
            await LoadPageDataAsync();
            return Page();
        }

        if (!ServiceOfferingId.HasValue)
        {
            ValidationErrors.Add("Please select a service.");
            await LoadPageDataAsync();
            return Page();
        }

        Guid? familyMemberId = BookForId == client.Id ? null : BookForId;

        var result = await _bookingService.CreateAppointmentAsync(
            BarberProfileId,
            client.Id,
            ServiceOfferingId.Value,
            slotStart,
            familyMemberId,
            User.IsInRole("Barber") ? BookingActorRole.Barber : BookingActorRole.Client);

        if (!result.IsSuccess)
        {
            ValidationErrors.Add(result.ErrorCode ?? "Booking failed.");
            await LoadPageDataAsync();
            return Page();
        }

        return RedirectToPage("/Booking/Confirm", new { appointmentId = result.AppointmentId });
    }

    private async Task LoadPageDataAsync()
    {
        var barber = await _db.BarberProfiles.FirstOrDefaultAsync(b => b.Id == BarberProfileId);
        BarberName = barber?.DisplayName ?? "Unknown Barber";

        Services = await _db.ServiceOfferings
            .Where(s => s.BarberProfileId == BarberProfileId && s.IsActive)
            .Select(s => new ServiceOption(s.Id, s.Name, s.DurationMinutes, s.PriceAmount))
            .ToListAsync();

        if (Date.HasValue && ServiceOfferingId.HasValue)
        {
            AvailableSlots = await _availability.GetAvailableSlotsAsync(
                BarberProfileId, ServiceOfferingId.Value, Date.Value);
        }

        var userId = _userManager.GetUserId(User);
        var client = await _db.ClientProfiles
            .Include(c => c.FamilyMembers)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (client != null)
        {
            FamilyMembers.Add(new FamilyMemberOption(client.Id, "Myself"));
            FamilyMembers.AddRange(client.FamilyMembers.Select(f => new FamilyMemberOption(f.Id, f.DisplayName)));
        }
    }
}

public record ServiceOption(Guid Id, string Name, int DurationMinutes, decimal Price);
public record FamilyMemberOption(Guid Id, string DisplayName);
