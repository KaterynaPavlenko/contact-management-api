namespace ContactManagement.Api.Entities;

public class Contact
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ContactCustomFieldValue> CustomFieldValues { get; set; } = new List<ContactCustomFieldValue>();
}
