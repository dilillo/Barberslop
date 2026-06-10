using Barberslop.Web.Data;
using Barberslop.Web.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Pages.Schedule;

[Authorize(Policy = "RequireBarber")]
public class AvailabilityModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public AvailabilityModel(ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<AvailabilityRule> WeeklyRules { get; set; } = new();
    public List<VacationPeriod> VacationPeriods { get; set; } = new();

    public async Task OnGetAsync()
    {
        var barber = await GetBarberAsync();
        if (barber == null) return;

        WeeklyRules = await _db.AvailabilityRules
            .Where(r => r.BarberProfileId == barber.Id)
            .OrderBy(r => r.DayOfWeek).ThenBy(r => r.StartTime)
            .ToListAsync();

        VacationPeriods = await _db.VacationPeriods
            .Where(v => v.BarberProfileId == barber.Id)
            .OrderBy(v => v.StartDate)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAddRuleAsync(
        int dayOfWeek, string startTime, string endTime, string effectiveFrom, string timeZoneId)
    {
        var barber = await GetBarberAsync();
        if (barber == null) return RedirectToPage();

        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            DayOfWeek = (DayOfWeek)dayOfWeek,
            StartTime = TimeOnly.Parse(startTime),
            EndTime = TimeOnly.Parse(endTime),
            EffectiveFrom = DateOnly.Parse(effectiveFrom),
            TimeZoneId = timeZoneId
        };

        _db.AvailabilityRules.Add(rule);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddVacationAsync(
        string startDate, string endDate, string? reason)
    {
        var barber = await GetBarberAsync();
        if (barber == null) return RedirectToPage();

        var vacation = new VacationPeriod
        {
            Id = Guid.NewGuid(),
            BarberProfileId = barber.Id,
            StartDate = DateOnly.Parse(startDate),
            EndDate = DateOnly.Parse(endDate),
            Reason = reason
        };

        _db.VacationPeriods.Add(vacation);
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task<BarberProfile?> GetBarberAsync()
    {
        var userId = _userManager.GetUserId(User);
        return await _db.BarberProfiles.FirstOrDefaultAsync(b => b.UserId == userId);
    }
}
