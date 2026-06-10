namespace Barberslop.Web.Domain;

public class FamilyMember
{
    public Guid Id { get; set; }
    public Guid ClientProfileId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ClientProfile ClientProfile { get; set; } = null!;
}
