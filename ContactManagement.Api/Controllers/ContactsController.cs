using ContactManagement.Api.Models;
using ContactManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContactManagement.Api.Controllers;

[ApiController]
[Route("api/contacts")]
public class ContactsController(IContactService contactService, ICustomFieldService customFieldService) : ControllerBase
{
    /// <summary>Get a contact by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var contact = await contactService.GetByIdAsync(id, cancellationToken);
        if (contact is null)
            return NotFound();
        return Ok(contact);
    }

    /// <summary>List contacts with pagination, sorting and optional email filter.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ContactResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] string? email = null,
        CancellationToken cancellationToken = default)
    {
        var query = new ContactListQuery { Page = page, PageSize = pageSize, SortBy = sortBy, SortOrder = sortOrder, Email = email };
        var result = await contactService.GetListAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Create a contact.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ContactResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateContactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contact = await contactService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = contact.Id }, contact);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Bulk merge contacts: match by email, update/create/delete in one transaction.</summary>
    [HttpPost("bulk-merge")]
    [ProducesResponseType(typeof(BulkMergeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkMerge([FromBody] BulkMergeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await contactService.BulkMergeAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Assign a custom field value to a contact.</summary>
    [HttpPut("{id:int}/custom-fields/{fieldId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignCustomFieldValue(int id, int fieldId, [FromBody] AssignCustomFieldValueRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var assigned = await customFieldService.AssignValueAsync(id, fieldId, request, cancellationToken);
            if (!assigned)
                return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update a contact.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var contact = await contactService.UpdateAsync(id, request, cancellationToken);
            if (contact is null)
                return NotFound();
            return Ok(contact);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete a contact.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await contactService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
