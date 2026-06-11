using Barberslop.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Barberslop.Web.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<BarberProfile> BarberProfiles => Set<BarberProfile>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<ServiceOffering> ServiceOfferings => Set<ServiceOffering>();
    public DbSet<AvailabilityRule> AvailabilityRules => Set<AvailabilityRule>();
    public DbSet<VacationPeriod> VacationPeriods => Set<VacationPeriod>();
    public DbSet<TemporaryUnavailability> TemporaryUnavailabilities => Set<TemporaryUnavailability>();
    public DbSet<InvitationRequest> InvitationRequests => Set<InvitationRequest>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ReminderDispatch> ReminderDispatches => Set<ReminderDispatch>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BarberProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.InviteCode).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SalonName).HasMaxLength(150);
            entity.Property(e => e.InviteCode).HasMaxLength(8).IsRequired();
            entity.Property(e => e.DefaultBookingLimit).HasDefaultValue(1);
        });

        builder.Entity<ClientProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(254).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
        });

        builder.Entity<FamilyMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Relationship).HasMaxLength(50);
            entity.HasOne(e => e.ClientProfile)
                .WithMany(c => c.FamilyMembers)
                .HasForeignKey(e => e.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ServiceOffering>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.PriceAmount).HasPrecision(10, 2);
            entity.Property(e => e.PriceCurrency).HasMaxLength(3).HasDefaultValue("USD");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(e => e.BarberProfile)
                .WithMany(b => b.ServiceOfferings)
                .HasForeignKey(e => e.BarberProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AvailabilityRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.HasOne(e => e.BarberProfile)
                .WithMany(b => b.AvailabilityRules)
                .HasForeignKey(e => e.BarberProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<VacationPeriod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.HasOne(e => e.BarberProfile)
                .WithMany(b => b.VacationPeriods)
                .HasForeignKey(e => e.BarberProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TemporaryUnavailability>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.HasOne(e => e.BarberProfile)
                .WithMany(b => b.TemporaryUnavailabilities)
                .HasForeignKey(e => e.BarberProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvitationRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.InitiatedBy).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.DisinviteReason).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(e => e.BarberProfile)
                .WithMany(b => b.InvitationRequests)
                .HasForeignKey(e => e.BarberProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ClientProfile)
                .WithMany(c => c.InvitationRequests)
                .HasForeignKey(e => e.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.BookedByRole).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.CancellationReason).HasMaxLength(300);
            entity.Property(e => e.StandingRecurrence).HasMaxLength(500);
            entity.HasOne(e => e.BarberProfile)
                .WithMany(b => b.Appointments)
                .HasForeignKey(e => e.BarberProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ClientProfile)
                .WithMany(c => c.Appointments)
                .HasForeignKey(e => e.ClientProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.FamilyMember)
                .WithMany()
                .HasForeignKey(e => e.FamilyMemberId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ServiceOffering)
                .WithMany()
                .HasForeignKey(e => e.ServiceOfferingId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ReminderDispatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(10);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.ExternalMessageId).HasMaxLength(255);
            entity.HasOne(e => e.Appointment)
                .WithMany(a => a.ReminderDispatches)
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
