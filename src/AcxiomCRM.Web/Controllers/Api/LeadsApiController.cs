using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Produces("application/json")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsApiController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? assignedTo, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _leadService.GetLeadsAsync(search, status, assignedTo, page, pageSize);
        return Ok(ApiResponse<PagedResult<LeadDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
            return NotFound(ApiResponse<object>.Fail($"Lead with ID {id} was not found."));

        return Ok(ApiResponse<LeadDto>.Ok(lead));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeadCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var result = await _leadService.CreateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        var createdLead = await _leadService.GetByIdAsync(result.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<LeadDto>.Ok(createdLead!, result.Message));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] LeadCreateUpdateDto dto)
    {
        dto.LeadId = id;

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var result = await _leadService.UpdateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, result.Message));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _leadService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, result.Message));
    }

    [HttpPost("{id:int}/convert")]
    public async Task<IActionResult> Convert(int id, [FromBody] LeadConvertDto dto)
    {
        dto.LeadId = id;

        var result = await _leadService.ConvertLeadAsync(dto);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { LeadId = id }, result.Message));
    }
}
