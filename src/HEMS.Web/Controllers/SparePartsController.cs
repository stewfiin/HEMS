using HEMS.Application.DTOs;
using HEMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HEMS.Web.Controllers;

[ApiController]
[Route("api/v1/spare-parts")]
public class SparePartsController : ControllerBase
{
    private readonly ISparePartService _sparePartService;

    public SparePartsController(ISparePartService sparePartService)
    {
        _sparePartService = sparePartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _sparePartService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var result = await _sparePartService.SearchAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _sparePartService.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Spare part tidak ditemukan." });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSparePartRequest request)
    {
        var result = await _sparePartService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}

