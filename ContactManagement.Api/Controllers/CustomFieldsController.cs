using ContactManagement.Api.Models;
using ContactManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContactManagement.Api.Controllers;

[ApiController]
[Route("api/custom-fields")]
public class CustomFieldsController : ControllerBase
{
    private readonly ICustomFieldService _customFieldService;

    public CustomFieldsController(ICustomFieldService customFieldService)
    {
        _customFieldService = customFieldService;
    }

    /// <summary>List all custom field definitions.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomFieldDefinitionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var list = await _customFieldService.GetListAsync(cancellationToken);
        return Ok(list);
    }

    /// <summary>Get a custom field definition by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CustomFieldDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var definition = await _customFieldService.GetByIdAsync(id, cancellationToken);
        if (definition is null)
            return NotFound();
        return Ok(definition);
    }

    /// <summary>Create a custom field definition.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CustomFieldDefinitionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomFieldRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var definition = await _customFieldService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = definition.Id }, definition);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Delete a custom field definition (cascades to contact values).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _customFieldService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();
        return NoContent();
    }
}
