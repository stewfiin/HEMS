using HEMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HEMS.Web.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] bool refresh = false)
    {
        var summary = await _dashboardService.GetSummaryAsync(refresh);
        return Ok(summary);
    }
}

