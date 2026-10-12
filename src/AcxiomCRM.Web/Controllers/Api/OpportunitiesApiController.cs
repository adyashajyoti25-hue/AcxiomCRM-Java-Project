using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Produces("application/json")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesApiController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? stage, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _opportunityService.GetOpportunitiesAsync(search, stage, status, page, pageSize);
        return Ok(ApiResponse<PagedResult<OpportunityDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id);
        if (opp == null)
            return NotFound(ApiResponse<object>.Fail($"Opportunity with ID {id} was not found."));

        return Ok(ApiResponse<OpportunityDto>.Ok(opp));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OpportunityCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var result = await _opportunityService.CreateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        var created = await _opportunityService.GetByIdAsync(result.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<OpportunityDto>.Ok(created!, result.Message));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] OpportunityCreateUpdateDto dto)
    {
        dto.OpportunityId = id;

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var result = await _opportunityService.UpdateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, result.Message));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _opportunityService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, result.Message));
    }
}
