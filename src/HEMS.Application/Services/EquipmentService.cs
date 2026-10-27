using HEMS.Application.DTOs;
using HEMS.Application.Interfaces;
using HEMS.Domain.Entities;
using HEMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HEMS.Application.Services;

public class EquipmentService : IEquipmentService
{
    private readonly IApplicationDbContext _context;

    public EquipmentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EquipmentDto>> GetAllAsync(string? siteLocation = null, string? status = null)
    {
        var query = _context.Equipments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(siteLocation))
            query = query.Where(e => e.SiteLocation == siteLocation);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<EquipmentStatus>(status, true, out var parsedStatus))
            query = query.Where(e => e.Status == parsedStatus);

        var list = await query.ToListAsync();

        return list.Select(e =>
        {
            // Cek aturan bisnis: kelipatan 250 HM dan sisa <= 25 jam
            decimal hmSinceLastService = e.CurrentHourMeter - e.LastServiceHourMeter;
            decimal hoursToNextService = 250 - (hmSinceLastService % 250);
            bool alert = hoursToNextService <= 25 || hmSinceLastService >= 250;

            return new EquipmentDto(
                e.Id,
                e.EquipmentCode,
                e.SerialNumber,
                e.Brand,
                e.Model,
                e.CurrentHourMeter,
                e.LastServiceHourMeter,
                e.Status.ToString(),
                e.SiteLocation,
                alert
            );
        });
    }

    public async Task<EquipmentDto?> GetByIdAsync(int id)
    {
        var e = await _context.Equipments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return null;

        decimal hmSinceLastService = e.CurrentHourMeter - e.LastServiceHourMeter;
        decimal hoursToNextService = 250 - (hmSinceLastService % 250);
        bool alert = hoursToNextService <= 25 || hmSinceLastService >= 250;

        return new EquipmentDto(
            e.Id,
            e.EquipmentCode,
            e.SerialNumber,
            e.Brand,
            e.Model,
            e.CurrentHourMeter,
            e.LastServiceHourMeter,
            e.Status.ToString(),
            e.SiteLocation,
            alert
        );
    }

    public async Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request)
    {
        var equipment = new Equipment
        {
            EquipmentCode = request.EquipmentCode,
            SerialNumber = request.SerialNumber,
            Brand = request.Brand,
            Model = request.Model,
            CurrentHourMeter = request.InitialHourMeter,
            LastServiceHourMeter = 0,
            Status = EquipmentStatus.Operating,
            SiteLocation = request.SiteLocation
        };

        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(equipment.Id))!;
    }

    public async Task<LogHourMeterResponse> RecordHourMeterAsync(int equipmentId, LogHourMeterRequest request)
    {
        var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (equipment == null)
            return new LogHourMeterResponse(false, "Unit tidak ditemukan.", 0, 0, false);

        // Aturan FR-1.3: HM baru tidak boleh lebih kecil dari HM sekarang
        if (request.NewHourMeter < equipment.CurrentHourMeter)
        {
            return new LogHourMeterResponse(
                false,
                $"Validasi Gagal: Angka HM baru ({request.NewHourMeter}) tidak boleh lebih kecil dari HM terakhir ({equipment.CurrentHourMeter}).",
                equipment.CurrentHourMeter,
                request.NewHourMeter,
                false
            );
        }

        decimal previousHM = equipment.CurrentHourMeter;
        equipment.CurrentHourMeter = request.NewHourMeter;

        // Catat riwayat log
        var log = new HourMeterLog
        {
            EquipmentId = equipment.Id,
            RecordedDate = DateTime.UtcNow,
            PreviousHM = previousHM,
            CurrentHM = request.NewHourMeter,
            RecordedBy = request.RecordedBy
        };

        _context.HourMeterLogs.Add(log);

        // Aturan FR-1.4: Cek ambang batas servis (kelipatan 250 jam)
        decimal hmSinceLastService = equipment.CurrentHourMeter - equipment.LastServiceHourMeter;
        decimal hoursToNextService = 250 - (hmSinceLastService % 250);
        bool needAlert = hoursToNextService <= 25 || hmSinceLastService >= 250;

        await _context.SaveChangesAsync();

        string message = needAlert
            ? $"Hour meter berhasil dicatat! PERINGATAN: Unit mendekati/melewati jadwal servis berkala kelipatan 250 HM (Sisa: {hoursToNextService} jam)."
            : "Hour meter berhasil dicatat.";

        return new LogHourMeterResponse(true, message, previousHM, equipment.CurrentHourMeter, needAlert);
    }
}