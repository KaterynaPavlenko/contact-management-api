namespace ContactManagement.Api.Entities;

public class ContactCustomFieldValue
{
    public int ContactId { get; set; }
    public Contact Contact { get; set; } = null!;

    public int CustomFieldDefinitionId { get; set; }
    public CustomFieldDefinition CustomFieldDefinition { get; set; } = null!;

    public string? ValueString { get; set; }
    public int? ValueInt { get; set; }
    public bool? ValueBool { get; set; }
}
