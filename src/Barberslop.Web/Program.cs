using Barberslop.Web.Data;
using Barberslop.Web.Features.Booking;
using Barberslop.Web.Features.Invitation;
using Barberslop.Web.Features.Reminders;
using Barberslop.Web.Features.Schedule;
using Barberslop.Web.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add PostgreSQL via Aspire (skip in testing)
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddNpgsqlDbContext<ApplicationDbContext>("barberslop");
}
else
{
    // In testing, the test host provides its own DbContext configuration
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("testing"));
}

// Add Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Add authorization policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireBarber", policy => policy.RequireRole("Barber"))
    .AddPolicy("RequireClient", policy => policy.RequireRole("Client"));

// Add application services
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IReminderDispatchService, ReminderDispatchService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<InvitationService>();

// Add reminder channels
builder.Services.AddScoped<IReminderChannel, EmailReminderChannel>();
builder.Services.AddScoped<IReminderChannel, SmsReminderChannel>();
builder.Services.AddScoped<IReminderChannel, CalendarInviteReminderChannel>();
builder.Services.AddScoped<IReminderChannel, PushNotificationReminderChannel>();

// Add background services
builder.Services.AddHostedService<ReminderDispatchHostedService>();

builder.Services.AddRazorPages();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }

    // Seed roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Barber", "Client" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program { }
