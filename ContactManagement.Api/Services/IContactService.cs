using ContactManagement.Api.Models;

namespace ContactManagement.Api.Services;

public interface IContactService
{
    Task<ContactResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<ContactResponse>> GetListAsync(ContactListQuery query, CancellationToken cancellationToken = default);
    Task<ContactResponse> CreateAsync(CreateContactRequest request, CancellationToken cancellationToken = default);
    Task<ContactResponse?> UpdateAsync(int id, UpdateContactRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<BulkMergeResult> BulkMergeAsync(BulkMergeRequest request, CancellationToken cancellationToken = default);
}
