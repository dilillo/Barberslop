using Barberslop.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Pages.Invitation;

[Authorize(Policy = "RequireClient")]
public class DiscoverModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public DiscoverModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? BarberName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SalonName { get; set; }

    public List<BarberSearchResult> Results { get; set; } = new();
    public bool SearchPerformed { get; set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(BarberName) && string.IsNullOrWhiteSpace(SalonName))
            return;

        SearchPerformed = true;

        var query = _db.BarberProfiles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(BarberName))
            query = query.Where(b => b.DisplayName.Contains(BarberName));

        if (!string.IsNullOrWhiteSpace(SalonName))
            query = query.Where(b => b.SalonName != null && b.SalonName.Contains(SalonName));

        Results = await query
            .Select(b => new BarberSearchResult(b.Id, b.DisplayName, b.SalonName))
            .Take(20)
            .ToListAsync();
    }
}

public record BarberSearchResult(Guid Id, string DisplayName, string? SalonName);
