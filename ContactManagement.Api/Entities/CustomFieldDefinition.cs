namespace ContactManagement.Api.Entities;

public class CustomFieldDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CustomFieldDataType DataType { get; set; }

    public ICollection<ContactCustomFieldValue> ContactValues { get; set; } = new List<ContactCustomFieldValue>();
}
