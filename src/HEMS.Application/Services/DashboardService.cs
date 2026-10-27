using System.Text.Json;
using HEMS.Application.DTOs;
using HEMS.Application.Interfaces;
using HEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace HEMS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private const string CacheKey = "dashboard_summary";

    public DashboardService(IApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(bool forceRefresh = false)
    {
        if (!forceRefresh)
        {
            try
            {
                var cachedBytes = await _cache.GetStringAsync(CacheKey);
                if (!string.IsNullOrEmpty(cachedBytes))
                {
                    var cachedData = JsonSerializer.Deserialize<DashboardSummaryDto>(cachedBytes);
                    if (cachedData != null)
                    {
                        return cachedData with { FromCache = true };
                    }
                }
            }
            catch
            {
                // Fallback jika Redis offline / tidak terjangkau
            }
        }

        // Kalkulasi Statistik dari Database
        var totalEquipment = await _context.Equipments.CountAsync();
        var operatingCount = await _context.Equipments.CountAsync(e => e.Status == EquipmentStatus.Operating);
        var breakdownCount = await _context.Equipments.CountAsync(e => e.Status == EquipmentStatus.Breakdown);
        var maintenanceCount = await _context.Equipments.CountAsync(e => e.Status == EquipmentStatus.UnderMaintenance);

        decimal physicalAvailability = totalEquipment > 0
            ? Math.Round(((decimal)operatingCount / totalEquipment) * 100, 2)
            : 0;

        var activeWorkOrders = await _context.WorkOrders.CountAsync(w => 
            w.Status != WorkOrderStatus.Closed && w.Status != WorkOrderStatus.Completed);

        // Top 5 Unit dengan riwayat Breakdown terbanyak
        var topBreakdowns = await _context.WorkOrders
            .Where(w => w.MaintenanceType == MaintenanceType.Corrective)
            .GroupBy(w => new { w.Equipment!.EquipmentCode, w.Equipment.Model })
            .Select(g => new TopBreakdownEquipmentDto(g.Key.EquipmentCode, g.Key.Model, g.Count()))
            .OrderByDescending(x => x.BreakdownCount)
            .Take(5)
            .ToListAsync();

        var summary = new DashboardSummaryDto(
            totalEquipment,
            operatingCount,
            breakdownCount,
            maintenanceCount,
            physicalAvailability,
            activeWorkOrders,
            topBreakdowns,
            FromCache: false
        );

        // Simpan ke Redis Cache selama 5 Menit sesuai PRD Bab 4
        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(summary), cacheOptions);
        }
        catch
        {
            // Abaikan jika Redis write gagal
        }

        return summary;
    }

    public async Task InvalidateDashboardCacheAsync()
    {
        try
        {
            await _cache.RemoveAsync(CacheKey);
        }
        catch
        {
            // Abaikan error cache removal
        }
    }
}
