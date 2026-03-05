using System.ComponentModel.DataAnnotations;
using ContactManagement.Api.Entities;

namespace ContactManagement.Api.Models;

public record CreateCustomFieldRequest(
    [Required][MinLength(1)][MaxLength(200)] string Name,
    CustomFieldDataType DataType
);
