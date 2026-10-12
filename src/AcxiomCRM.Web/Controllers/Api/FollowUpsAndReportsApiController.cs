using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Produces("application/json")]
[Authorize]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;

    public FollowUpsApiController(IFollowUpService followUpService)
    {
        _followUpService = followUpService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? type, [FromQuery] DateTime? date, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _followUpService.GetFollowUpsAsync(search, status, type, date, page, pageSize);
        return Ok(ApiResponse<PagedResult<FollowUpDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var f = await _followUpService.GetByIdAsync(id);
        if (f == null)
            return NotFound(ApiResponse<object>.Fail($"Follow-up with ID {id} was not found."));

        return Ok(ApiResponse<FollowUpDto>.Ok(f));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FollowUpCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var result = await _followUpService.CreateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        var created = await _followUpService.GetByIdAsync(result.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<FollowUpDto>.Ok(created!, result.Message));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] FollowUpCreateUpdateDto dto)
    {
        dto.FollowUpId = id;

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var result = await _followUpService.UpdateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, result.Message));
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] string? remarks)
    {
        var result = await _followUpService.CompleteAsync(id, remarks);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, result.Message));
    }
}

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport()
    {
        var data = await _reportService.GetPipelineReportAsync();
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("conversion")]
    public async Task<IActionResult> GetConversionReport()
    {
        var data = await _reportService.GetConversionReportAsync();
        return Ok(ApiResponse<object>.Ok(data));
    }
}
