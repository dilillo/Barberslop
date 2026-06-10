using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Pages.Services;

[Authorize(Policy = "RequireBarber")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<ServiceOffering> ServicesList { get; set; } = new();

    public async Task OnGetAsync()
    {
        var barber = await GetBarberAsync();
        if (barber == null) return;

        ServicesList = await _db.ServiceOfferings
            .Where(s => s.BarberProfileId == barber.Id)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(string name, string? description, int durationMinutes, decimal priceAmount)
    {
        var barber = await GetBarberAsync();
        if (barber == null) return RedirectToPage();

        var service = new ServiceOffering
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            Name = name,
            Description = description,
            DurationMinutes = durationMinutes,
            PriceAmount = priceAmount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ServiceOfferings.Add(service);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<BarberProfile?> GetBarberAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _db.BarberProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
    }
}
