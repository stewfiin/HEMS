using HEMS.Application.DTOs;
using HEMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HEMS.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrderService _workOrderService;

    public WorkOrdersController(IWorkOrderService workOrderService)
    {
        _workOrderService = workOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var result = await _workOrderService.GetAllAsync(status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _workOrderService.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Work Order tidak ditemukan." });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request)
    {
        try
        {
            var result = await _workOrderService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteWorkOrderRequest request)
    {
        var response = await _workOrderService.CompleteWorkOrderAsync(id, request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPut("{id}/close")]
    public async Task<IActionResult> Close(int id)
    {
        var success = await _workOrderService.CloseWorkOrderAsync(id);
        if (!success) return NotFound(new { message = "Work Order tidak ditemukan." });
        return Ok(new { message = "Work Order berhasil ditutup dan status unit kembali Operating." });
    }
}
