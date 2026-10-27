using HEMS.Application.DTOs;
using HEMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HEMS.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class EquipmentsController : ControllerBase
{
    private readonly IEquipmentService _equipmentService;

    public EquipmentsController(IEquipmentService equipmentService)
    {
        _equipmentService = equipmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? siteLocation, [FromQuery] string? status)
    {
        var result = await _equipmentService.GetAllAsync(siteLocation, status);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _equipmentService.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Equipment not found" });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEquipmentRequest request)
    {
        var result = await _equipmentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id}/hm-logs")]
    public async Task<IActionResult> RecordHourMeter(int id, [FromBody] LogHourMeterRequest request)
    {
        var response = await _equipmentService.RecordHourMeterAsync(id, request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }
}