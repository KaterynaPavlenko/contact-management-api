namespace ContactManagement.Api.Models;

/// <summary>Exactly one of ValueString, ValueInt, ValueBool must be set, matching the field's DataType.</summary>
public record AssignCustomFieldValueRequest(string? ValueString, int? ValueInt, bool? ValueBool);
