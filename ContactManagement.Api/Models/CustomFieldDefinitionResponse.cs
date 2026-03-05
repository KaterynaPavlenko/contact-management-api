using ContactManagement.Api.Entities;

namespace ContactManagement.Api.Models;

public record CustomFieldDefinitionResponse(int Id, string Name, CustomFieldDataType DataType);
