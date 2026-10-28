using HEMS.Application.DTOs;
using HEMS.Application.Services;
using HEMS.Domain.Entities;
using HEMS.Domain.Enums;
using HEMS.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace HEMS.Tests;

public class SystemIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public SystemIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SIT_01_CompleteWorkOrder_RollsBack_WhenSparePartStockIsInsufficient()
    {
        // Arrange
        var equipment = new Equipment
        {
            EquipmentCode = "EX-TEST",
            SerialNumber = "SN-TEST-001",
            Brand = "Komatsu",
            Model = "PC200",
            CurrentHourMeter = 100,
            LastServiceHourMeter = 0,
            Status = EquipmentStatus.Operating,
            SiteLocation = "Site Test"
        };
        _context.Equipments.Add(equipment);

        var sparePart = new SparePart
        {
            PartNumber = "FLT-001",
            PartName = "Oil Filter",
            StockQuantity = 2, // Hanya ada 2 pcs di gudang
            UnitPrice = 50000
        };
        _context.SpareParts.Add(sparePart);
        await _context.SaveChangesAsync();

        var workOrder = new WorkOrder
        {
            WorkOrderNumber = "WO-TEST-001",
            EquipmentId = equipment.Id,
            MaintenanceType = MaintenanceType.Corrective,
            ProblemDescription = "Ganti Filter",
            Status = WorkOrderStatus.InProgress
        };
        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();

        var service = new WorkOrderService(_context);

        // Act: Mencoba meminta 5 pcs padahal stok hanya 2
        var request = new CompleteWorkOrderRequest(new List<AddPartToWorkOrderRequest>
        {
            new(sparePart.Id, Quantity: 5)
        });

        var response = await service.CompleteWorkOrderAsync(workOrder.Id, request);

        // Assert: SIT-01 membuktikan bahwa transaksi di-rollback secara atomik
        Assert.False(response.Success);
        Assert.Contains("tidak mencukupi", response.Message);

        // Verifikasi database: Status WO TIDAK berubah menjadi Completed, dan stok TIDAK berkurang
        var freshWO = await _context.WorkOrders.FindAsync(workOrder.Id);
        var freshPart = await _context.SpareParts.FindAsync(sparePart.Id);

        Assert.Equal(WorkOrderStatus.InProgress, freshWO!.Status);
        Assert.Equal(2, freshPart!.StockQuantity); // Stok tetap utuh 2
    }

    [Fact]
    public async Task SIT_02_DashboardCache_IsInvalidated_Properly()
    {
        // Arrange
        var mockCache = new Mock<IDistributedCache>();
        var service = new DashboardService(_context, mockCache.Object);

        // Act
        await service.InvalidateDashboardCacheAsync();

        // Assert: SIT-02 membuktikan bahwa key cache dashboard di-remove dari Redis
        mockCache.Verify(c => c.RemoveAsync("dashboard_summary", default), Times.Once);
    }
}

