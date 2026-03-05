using ContactManagement.Api.Models;

namespace ContactManagement.Api.Services;

public interface ICustomFieldService
{
    Task<IReadOnlyList<CustomFieldDefinitionResponse>> GetListAsync(CancellationToken cancellationToken = default);
    Task<CustomFieldDefinitionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CustomFieldDefinitionResponse> CreateAsync(CreateCustomFieldRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AssignValueAsync(int contactId, int fieldId, AssignCustomFieldValueRequest request, CancellationToken cancellationToken = default);
}
