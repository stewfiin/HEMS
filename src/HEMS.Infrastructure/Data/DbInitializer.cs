using HEMS.Domain.Entities;
using HEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HEMS.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Pastikan database siap
        await context.Database.MigrateAsync();

        // 1. Seed Equipment jika belum ada
        if (!await context.Equipments.AnyAsync())
        {
            var equipments = new List<Equipment>
            {
                new()
                {
                    EquipmentCode = "EX-2001",
                    SerialNumber = "KMTPC200-88901",
                    Brand = "Komatsu",
                    Model = "PC200-8",
                    CurrentHourMeter = 240.00m,
                    LastServiceHourMeter = 0.00m,
                    Status = EquipmentStatus.Operating,
                    SiteLocation = "Site Batukajang"
                },
                new()
                {
                    EquipmentCode = "DZ-1002",
                    SerialNumber = "CATD8R-55412",
                    Brand = "Caterpillar",
                    Model = "D8R Dozer",
                    CurrentHourMeter = 980.50m,
                    LastServiceHourMeter = 750.00m,
                    Status = EquipmentStatus.Operating,
                    SiteLocation = "Site Sangatta"
                },
                new()
                {
                    EquipmentCode = "DT-3005",
                    SerialNumber = "SCN-G460-11234",
                    Brand = "Scania",
                    Model = "G460 Tipper",
                    CurrentHourMeter = 1450.00m,
                    LastServiceHourMeter = 1250.00m,
                    Status = EquipmentStatus.Breakdown,
                    SiteLocation = "Site Tutupan"
                }
            };

            await context.Equipments.AddRangeAsync(equipments);
        }

        // 2. Seed Spare Parts jika belum ada
        if (!await context.SpareParts.AnyAsync())
        {
            var spareParts = new List<SparePart>
            {
                new()
                {
                    PartNumber = "6732-71-6111",
                    PartName = "Engine Oil Filter (Komatsu)",
                    StockQuantity = 45,
                    UnitPrice = 450000m
                },
                new()
                {
                    PartNumber = "1R-0716",
                    PartName = "Fuel Filter Water Separator (CAT)",
                    StockQuantity = 30,
                    UnitPrice = 680000m
                },
                new()
                {
                    PartNumber = "07063-01100",
                    PartName = "Hydraulic Return Filter Element",
                    StockQuantity = 15,
                    UnitPrice = 1250000m
                }
            };

            await context.SpareParts.AddRangeAsync(spareParts);
        }

        await context.SaveChangesAsync();
    }
}