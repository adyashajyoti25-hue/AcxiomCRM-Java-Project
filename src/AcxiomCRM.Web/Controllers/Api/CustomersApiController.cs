using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Produces("application/json")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _customerService.GetCustomersAsync(search, status, page, pageSize);
        return Ok(ApiResponse<PagedResult<CustomerDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound(ApiResponse<object>.Fail($"Customer with ID {id} was not found."));

        return Ok(ApiResponse<CustomerDto>.Ok(customer));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerCreateUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var result = await _customerService.CreateAsync(dto);
        if (!result.Success)
        {
            return Conflict(ApiResponse<object>.Fail(result.Message));
        }

        var createdCustomer = await _customerService.GetByIdAsync(result.Id);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<CustomerDto>.Ok(createdCustomer!, result.Message));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerCreateUpdateDto dto)
    {
        dto.CustomerId = id;

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.Fail("Validation failed.", errors));
        }

        var result = await _customerService.UpdateAsync(dto);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, result.Message));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _customerService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { Id = id }, result.Message));
    }
}
