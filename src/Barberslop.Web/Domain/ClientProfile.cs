namespace Barberslop.Web.Domain;

public class ClientProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();
    public ICollection<InvitationRequest> InvitationRequests { get; set; } = new List<InvitationRequest>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
