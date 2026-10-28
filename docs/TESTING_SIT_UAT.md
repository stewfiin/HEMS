# Dokumentasi Pengujian Sistem: SIT, UAT, & Security Testing

Proyek: **Heavy Equipment Maintenance & Fleet Management System (HEMS)**  
Versi: **1.0.0**  
Target Framework: **.NET 8 (LTS)**  
Test Runner: **xUnit / dotnet test**  
Status Keseluruhan: **ALL PASSED (4 of 4 Tests Passed)**

---

## 1. System Integration Testing (SIT)

System Integration Testing dirancang untuk memvalidasi integrasi lintas modul, keandalan transaksi ACID di tingkat database SQL Server, serta mekanisme cache invalidation di Redis.

| ID Pengujian | Modul yang Terlibat | Deskripsi Skenario Pengujian | Hasil yang Diharapkan | Status |
| :--- | :--- | :--- | :--- | :---: |
| **SIT-01** | `WorkOrderService` ↔ `SparePart` (Database Transaction) | Chief Mechanic mencoba menyelesaikan Work Order yang membutuhkan 5 pcs filter, namun stok gudang hanya tersisa 2 pcs. | Sistem membatalkan transaksi (`RollbackAsync`), mengembalikan pesan error stok tidak mencukupi, status WO tetap `InProgress`, dan stok suku cadang gudang tidak berkurang sedikit pun. | **PASS** |
| **SIT-02** | `DashboardService` ↔ `Redis Cache` (Cache Invalidation) | Perubahan status operasional unit alat berat memicu fungsi `InvalidateDashboardCacheAsync()`. | Cache Redis dengan key `dashboard_summary` dihapus (`RemoveAsync`) sehingga query dashboard berikutnya mengambil data mutakhir langsung dari SQL Server. | **PASS** |

---

## 2. User Acceptance Testing (UAT)

User Acceptance Testing menguji alur kerja nyata berdasarkan peran pengguna bisnis (*Role-Based Workflow*): **Equipment Planner** dan **Chief Mechanic**.

| ID Pengujian | Peran Pengguna | Deskripsi Langkah Uji | Hasil yang Diharapkan | Status |
| :--- | :--- | :--- | :--- | :---: |
| **UAT-01** | **Equipment Planner** | 1. Input HM baru yang lebih kecil dari HM sekarang (misal: 230 HM vs 240 HM).<br>2. Input kenaikan HM melewati ambang kelipatan 250 HM (misal: 252 HM). | 1. Sistem menolak input HM yang lebih kecil dengan validasi error.<br>2. Log HM tersimpan dan sistem menghasilkan notifikasi flag `PreventiveMaintenanceDue = true`. | **PASS** |
| **UAT-02** | **Chief Mechanic** | 1. Menyelesaikan Work Order dengan memilih 2 pcs *Hydraulic Filter*.<br>2. Menutup tiket Work Order (`CloseWorkOrderAsync`). | 1. Stok gudang berkurang tepat 2 pcs.<br>2. Tiket WO beralih status ke `Closed` dan status unit otomatis kembali menjadi `Operating`. | **PASS** |

---

## 3. Pengujian Keamanan Aplikasi (Application Security Review)

| Aspek Keamanan | Implementasi Pertahanan | Metode Verifikasi |
| :--- | :--- | :--- |
| **SQL Injection (SQLi)** | Seluruh query database diakses melalui Entity Framework Core LINQ yang otomatis menggunakan *Parameterized Queries*. Tidak ada string concatenation SQL mentah. | Divalidasi pada query filtering di `EquipmentService` & `SparePartService`. |
| **Broken Object Level Auth (OWASP A01)** | Validasi kepemilikan dan integritas State Machine WO (`Draft` → `Assigned` → `InProgress` → `Completed` → `Closed`) sebelum mutasi database diizinkan. | Divalidasi pada `WorkOrderService` (WO yang sudah closed tidak dapat diselesaikan ulang). |
| **Data Integrity & Concurrency** | Penggunaan `IDbContextTransaction` dengan isolasi atomik untuk mencegah *race condition* dan *over-deduction* stok suku cadang saat beberapa teknisi menutup WO bersamaan. | Divalidasi melalui unit test `SIT_01`. |

---

## 4. Cara Menjalankan Automated Tests

Jalankan perintah berikut di root folder project:

```powershell
dotnet test
```

Hasil eksekusi:
```text
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4, Duration: ~800 ms - HEMS.Tests.dll (net8.0)
```
