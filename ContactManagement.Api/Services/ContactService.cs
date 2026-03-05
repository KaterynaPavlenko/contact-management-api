using ContactManagement.Api.Data;
using ContactManagement.Api.Entities;
using ContactManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactManagement.Api.Services;

public class ContactService(AppDbContext db): IContactService
{
    public async Task<ContactResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var contact = await db.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return contact is null ? null : MapToResponse(contact);
    }

    public async Task<PagedResult<ContactResponse>> GetListAsync(ContactListQuery query, CancellationToken cancellationToken = default)
    {
        var q = db.Contacts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Email))
            q = q.Where(c => c.Email != null && c.Email.Contains(query.Email));

        var totalCount = await q.CountAsync(cancellationToken);

        var sortBy = query.SortBy?.Trim() ?? "CreatedAt";
        var ascending = query.SortOrder?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true;
        var ordered = (sortBy, ascending) switch
        {
            ("Name", true) => q.OrderBy(c => c.Name),
            ("Name", false) => q.OrderByDescending(c => c.Name),
            ("Email", true) => q.OrderBy(c => c.Email),
            ("Email", false) => q.OrderByDescending(c => c.Email),
            ("Phone", true) => q.OrderBy(c => c.Phone),
            ("Phone", false) => q.OrderByDescending(c => c.Phone),
            ("UpdatedAt", true) => q.OrderBy(c => c.UpdatedAt),
            ("UpdatedAt", false) => q.OrderByDescending(c => c.UpdatedAt),
            (_, true) => q.OrderBy(c => c.CreatedAt),
            (_, false) => q.OrderByDescending(c => c.CreatedAt)
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContactResponse(c.Id, c.Name, c.Email, c.Phone, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<ContactResponse>(items, totalCount, page, pageSize);
    }

    public async Task<ContactResponse> CreateAsync(CreateContactRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (!string.IsNullOrEmpty(email) && await db.Contacts.AnyAsync(c => c.Email != null && c.Email.Trim().ToLower() == email.ToLower(), cancellationToken))
            throw new ArgumentException($"A contact with email '{email}' already exists.", nameof(request));

        var contact = new Contact
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Contacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);
        return MapToResponse(contact);
    }

    public async Task<ContactResponse?> UpdateAsync(int id, UpdateContactRequest request, CancellationToken cancellationToken = default)
    {
        var contact = await db.Contacts.FindAsync(new object[] { id }, cancellationToken);
        if (contact is null) return null;

        var email = request.Email?.Trim();
        if (!string.IsNullOrEmpty(email))
        {
            var duplicate = await db.Contacts.AnyAsync(c => c.Id != id && c.Email != null && c.Email.Trim().ToLower() == email.ToLower(), cancellationToken);
            if (duplicate)
                throw new ArgumentException($"A contact with email '{email}' already exists.", nameof(request));
        }

        contact.Name = request.Name;
        contact.Email = request.Email;
        contact.Phone = request.Phone;
        contact.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return MapToResponse(contact);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var contact = await db.Contacts.FindAsync(new object[] { id }, cancellationToken);
        if (contact is null) return false;
        db.Contacts.Remove(contact);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<BulkMergeResult> BulkMergeAsync(BulkMergeRequest request, CancellationToken cancellationToken = default)
    {
        var items = request.Items ?? Array.Empty<BulkMergeContactItem>();
        var inputEmails = items
            .Select(x => x.Email?.Trim())
            .Where(e => !string.IsNullOrEmpty(e))
            .ToList();
        var duplicateEmails = inputEmails
            .GroupBy(e => e!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateEmails.Count > 0)
            throw new ArgumentException($"Duplicate emails in input: {string.Join(", ", duplicateEmails)}.");

        var inputEmailSet = new HashSet<string>(inputEmails, StringComparer.OrdinalIgnoreCase);
        var createdIds = new List<int>();
        var updatedIds = new List<int>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existingByEmail = await db.Contacts
                .Where(c => c.Email != null && inputEmailSet.Contains(c.Email))
                .ToDictionaryAsync(c => c.Email!.Trim(), c => c, StringComparer.OrdinalIgnoreCase, cancellationToken);

            var newContacts = new List<Contact>();
            foreach (var item in items)
            {
                var email = item.Email?.Trim();
                if (string.IsNullOrEmpty(email))
                {
                    newContacts.Add(new Contact
                    {
                        Name = item.Name,
                        Email = null,
                        Phone = item.Phone,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    continue;
                }

                if (existingByEmail.TryGetValue(email, out var existing))
                {
                    existing.Name = item.Name;
                    existing.Email = email;
                    existing.Phone = item.Phone;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existingByEmail.Remove(email);
                    updatedIds.Add(existing.Id);
                }
                else
                {
                    newContacts.Add(new Contact
                    {
                        Name = item.Name,
                        Email = email,
                        Phone = item.Phone,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            db.Contacts.AddRange(newContacts);
            await db.SaveChangesAsync(cancellationToken);
            foreach (var c in newContacts)
                createdIds.Add(c.Id);

            var toDelete = await db.Contacts
                .Where(c => c.Email != null && !inputEmailSet.Contains(c.Email))
                .Select(c => new { c.Id })
                .ToListAsync(cancellationToken);
            var deletedIds = toDelete.Select(x => x.Id).ToList();
            if (toDelete.Count > 0)
            {
                await db.Contacts.Where(c => deletedIds.Contains(c.Id)).ExecuteDeleteAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return new BulkMergeResult(
                createdIds.Count,
                updatedIds.Count,
                deletedIds.Count,
                createdIds,
                updatedIds,
                deletedIds
            );
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static ContactResponse MapToResponse(Contact c) =>
        new(c.Id, c.Name, c.Email, c.Phone, c.CreatedAt, c.UpdatedAt);
}
