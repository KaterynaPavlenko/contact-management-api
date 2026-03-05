using ContactManagement.Api.Data;
using ContactManagement.Api.Entities;
using ContactManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactManagement.Api.Services;

public class CustomFieldService : ICustomFieldService
{
    private readonly AppDbContext _db;

    public CustomFieldService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomFieldDefinitionResponse>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CustomFieldDefinitions
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .Select(f => new CustomFieldDefinitionResponse(f.Id, f.Name, f.DataType))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomFieldDefinitionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var definition = await _db.CustomFieldDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        return definition is null ? null : new CustomFieldDefinitionResponse(definition.Id, definition.Name, definition.DataType);
    }

    public async Task<CustomFieldDefinitionResponse> CreateAsync(CreateCustomFieldRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Field name is required.", nameof(request));

        var name = request.Name.Trim();
        var exists = await _db.CustomFieldDefinitions.AnyAsync(f => f.Name == name, cancellationToken);
        if (exists)
            throw new ArgumentException($"A custom field with name '{name}' already exists.", nameof(request));

        var definition = new CustomFieldDefinition
        {
            Name = name,
            DataType = request.DataType
        };
        _db.CustomFieldDefinitions.Add(definition);
        await _db.SaveChangesAsync(cancellationToken);
        return new CustomFieldDefinitionResponse(definition.Id, definition.Name, definition.DataType);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var definition = await _db.CustomFieldDefinitions.FindAsync(new object[] { id }, cancellationToken);
        if (definition is null)
            return false;
        _db.CustomFieldDefinitions.Remove(definition);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AssignValueAsync(int contactId, int fieldId, AssignCustomFieldValueRequest request, CancellationToken cancellationToken = default)
    {
        var contact = await _db.Contacts.FindAsync(new object[] { contactId }, cancellationToken);
        if (contact is null)
            return false;

        var definition = await _db.CustomFieldDefinitions.FindAsync(new object[] { fieldId }, cancellationToken);
        if (definition is null)
            return false;

        ValidateAssignValue(definition.DataType, request);

        var existing = await _db.ContactCustomFieldValues
            .FirstOrDefaultAsync(v => v.ContactId == contactId && v.CustomFieldDefinitionId == fieldId, cancellationToken);

        if (existing is not null)
        {
            SetValue(existing, definition.DataType, request);
        }
        else
        {
            var value = new ContactCustomFieldValue
            {
                ContactId = contactId,
                CustomFieldDefinitionId = fieldId
            };
            SetValue(value, definition.DataType, request);
            _db.ContactCustomFieldValues.Add(value);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateAssignValue(CustomFieldDataType dataType, AssignCustomFieldValueRequest request)
    {
        switch (dataType)
        {
            case CustomFieldDataType.String:
                if (request.ValueString is null)
                    throw new ArgumentException("ValueString is required for a String custom field.", nameof(request));
                break;
            case CustomFieldDataType.Integer:
                if (!request.ValueInt.HasValue)
                    throw new ArgumentException("ValueInt is required for an Integer custom field.", nameof(request));
                break;
            case CustomFieldDataType.Bool:
                if (!request.ValueBool.HasValue)
                    throw new ArgumentException("ValueBool is required for a Bool custom field.", nameof(request));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
        }

        var hasOther = (dataType != CustomFieldDataType.String && request.ValueString is not null)
            || (dataType != CustomFieldDataType.Integer && request.ValueInt.HasValue)
            || (dataType != CustomFieldDataType.Bool && request.ValueBool.HasValue);
        if (hasOther)
            throw new ArgumentException("Only the value matching the field's DataType should be set.", nameof(request));
    }

    private static void SetValue(ContactCustomFieldValue value, CustomFieldDataType dataType, AssignCustomFieldValueRequest request)
    {
        value.ValueString = null;
        value.ValueInt = null;
        value.ValueBool = null;
        switch (dataType)
        {
            case CustomFieldDataType.String:
                value.ValueString = request.ValueString;
                break;
            case CustomFieldDataType.Integer:
                value.ValueInt = request.ValueInt;
                break;
            case CustomFieldDataType.Bool:
                value.ValueBool = request.ValueBool;
                break;
        }
    }
}
