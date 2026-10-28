using HEMS.Application.DTOs;
using HEMS.Application.Services;
using HEMS.Domain.Entities;
using HEMS.Domain.Enums;
using HEMS.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HEMS.Tests;

public class UserAcceptanceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public UserAcceptanceTests()
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
    public async Task UAT_01_EquipmentPlanner_LogsHourMeter_TriggersServiceAlert_AndRejectsLowerHM()
    {
        // Skenario UAT-01:
        // Equipment Planner menginput HM unit Komatsu PC200 dari 240 HM menjadi 252 HM
        // Sistem menampilkan notifikasi Preventive Maintenance 250 HM.
        var equipment = new Equipment
        {
            EquipmentCode = "EX-2001",
            SerialNumber = "KMTPC200-88901",
            Brand = "Komatsu",
            Model = "PC200-8",
            CurrentHourMeter = 240.00m,
            LastServiceHourMeter = 0.00m,
            Status = EquipmentStatus.Operating,
            SiteLocation = "Site Batukajang"
        };
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        var service = new EquipmentService(_context);

        // 1. Validasi bisnis menolak HM yang lebih kecil
        var invalidResponse = await service.RecordHourMeterAsync(equipment.Id, new LogHourMeterRequest(230.00m, "Planner-1"));
        Assert.False(invalidResponse.Success);
        Assert.Contains("tidak boleh lebih kecil", invalidResponse.Message);

        // 2. Input HM valid menjadi 252 HM -> Harus memicu alert servis kelipatan 250 HM
        var validResponse = await service.RecordHourMeterAsync(equipment.Id, new LogHourMeterRequest(252.00m, "Planner-1"));
        Assert.True(validResponse.Success);
        Assert.True(validResponse.PreventiveMaintenanceDue);
        Assert.Equal(252.00m, validResponse.CurrentHM);
        Assert.Equal(240.00m, validResponse.PreviousHM);

        // Pastikan terupdate di database
        var fresh = await _context.Equipments.FindAsync(equipment.Id);
        Assert.Equal(252.00m, fresh!.CurrentHourMeter);
    }

    [Fact]
    public async Task UAT_02_ChiefMechanic_UsesParts_CompletesWO_AndUnitReturnsToOperating()
    {
        // Skenario UAT-02:
        // Chief Mechanic menambahkan 2 buah Hydraulic Filter ke dalam WO dan menyelesaikan pekerjaan.
        // Stok gudang berkurang tepat 2 buah dan saat WO ditutup, status unit kembali Operating.
        var equipment = new Equipment
        {
            EquipmentCode = "DT-3005",
            SerialNumber = "SCN-G460-11234",
            Brand = "Scania",
            Model = "G460 Tipper",
            CurrentHourMeter = 1450.00m,
            LastServiceHourMeter = 1250.00m,
            Status = EquipmentStatus.Breakdown,
            SiteLocation = "Site Tutupan"
        };
        _context.Equipments.Add(equipment);

        var filterPart = new SparePart
        {
            PartNumber = "07063-01100",
            PartName = "Hydraulic Return Filter Element",
            StockQuantity = 15,
            UnitPrice = 1250000m
        };
        _context.SpareParts.Add(filterPart);
        await _context.SaveChangesAsync();

        var workOrder = new WorkOrder
        {
            WorkOrderNumber = "WO-202610-0001",
            EquipmentId = equipment.Id,
            MaintenanceType = MaintenanceType.Corrective,
            ProblemDescription = "Kebocoran hydraulic filter",
            Status = WorkOrderStatus.InProgress,
            AssignedMechanic = "Chief Mechanic"
        };
        _context.WorkOrders.Add(workOrder);
        await _context.SaveChangesAsync();

        var service = new WorkOrderService(_context);

        // 1. Selesaikan WO dengan konsumsi 2 buah part
        var completeRequest = new CompleteWorkOrderRequest(new List<AddPartToWorkOrderRequest>
        {
            new(filterPart.Id, Quantity: 2)
        });

        var completeRes = await service.CompleteWorkOrderAsync(workOrder.Id, completeRequest);
        Assert.True(completeRes.Success);

        // Verifikasi stok berkurang tepat 2 (15 - 2 = 13)
        var freshPart = await _context.SpareParts.FindAsync(filterPart.Id);
        Assert.Equal(13, freshPart!.StockQuantity);

        // 2. Tutup WO (Close)
        var closeRes = await service.CloseWorkOrderAsync(workOrder.Id);
        Assert.True(closeRes);

        // Verifikasi unit otomatis kembali ke status Operating
        var freshEq = await _context.Equipments.FindAsync(equipment.Id);
        Assert.Equal(EquipmentStatus.Operating, freshEq!.Status);
    }
}

