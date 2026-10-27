using HEMS.Application.DTOs;

namespace HEMS.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(bool forceRefresh = false);
    Task InvalidateDashboardCacheAsync();
}
