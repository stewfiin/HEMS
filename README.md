# Heavy Equipment Maintenance & Fleet Management System (HEMS)

[![.NET 8](https://img.shields.io/badge/.NET-8.0%20LTS-purple.svg)](https://dotnet.microsoft.com/)
[![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022-red.svg)](https://www.microsoft.com/sql-server)
[![Redis](https://img.shields.io/badge/Redis-7%20Cache-red.svg)](https://redis.io/)
[![Kendo UI](https://img.shields.io/badge/Telerik-Kendo%20UI-orange.svg)](https://www.telerik.com/kendo-ui)
[![Tests](https://img.shields.io/badge/Tests-All%20Passed%20(4/4)-brightgreen.svg)](docs/TESTING_SIT_UAT.md)

**HEMS** adalah sistem back-office manajemen armada alat berat pertambangan dan konstruksi berbasis enterprise. Dirancang menggunakan pola **Clean Architecture** pada ekosistem **C# .NET 8**, **Microsoft SQL Server**, **Redis Distributed Caching**, serta antarmuka dinamis **jQuery & Telerik Kendo UI**.

---

## 🏗️ 1. System Architecture Diagram

```mermaid
flowchart TD
    subgraph Client_Layer ["Client Layer (Browser)"]
        UI["Razor Pages + jQuery"]
        KENDO["Telerik Kendo UI Grid & Widgets"]
        SWAGGER["OpenAPI / Swagger UI"]
    end

    subgraph API_Gateway ["ASP.NET Core .NET 8 (HEMS.Web)"]
        AUTH["JWT / Keycloak Auth Middleware"]
        EQ_CTRL["Equipments Controller"]
        WO_CTRL["WorkOrders Controller"]
        SP_CTRL["SpareParts Controller"]
        DASH_CTRL["Dashboard Controller"]
    end

    subgraph Application_Layer ["Application & Domain Layer (HEMS.Core)"]
        EQ_SVC["EquipmentService (Business Rules & Alert)"]
        WO_SVC["WorkOrderService (Atomic Transactions)"]
        SP_SVC["SparePartService (Catalog & Search)"]
        DASH_SVC["DashboardService (PA & MTBF Metrics)"]
    end

    subgraph Persistence_Layer ["Infrastructure & Data Store (Docker Containers)"]
        REDIS[("Redis Cache\n- Dashboard Summary\n- Master Cache")]
        MSSQL[("Microsoft SQL Server 2022\n- Equipments\n- HourMeterLogs\n- WorkOrders\n- SpareParts & Pivot")]
    end

    UI --> API_Gateway
    KENDO --> API_Gateway
    SWAGGER --> API_Gateway

    API_Gateway --> Application_Layer
    Application_Layer --> REDIS
    Application_Layer --> MSSQL
```

---

## 🗄️ 2. Entity Relationship Diagram (ERD)

```mermaid
erDiagram
    EQUIPMENTS ||--o{ HOUR_METER_LOGS : "records"
    EQUIPMENTS ||--o{ WORK_ORDERS : "undergoes"
    WORK_ORDERS ||--o{ WORK_ORDER_PARTS : "contains"
    SPARE_PARTS ||--o{ WORK_ORDER_PARTS : "consumed_in"

    EQUIPMENTS {
        int Id PK
        string EquipmentCode "Unique (e.g. EX-2001)"
        string SerialNumber "Unique"
        string Brand
        string Model
        decimal CurrentHourMeter
        decimal LastServiceHourMeter
        string Status "Operating, Breakdown, Maintenance"
        string SiteLocation
    }

    HOUR_METER_LOGS {
        bigint Id PK
        int EquipmentId FK
        datetime RecordedDate
        decimal PreviousHM
        decimal CurrentHM
        string RecordedBy
    }

    WORK_ORDERS {
        int Id PK
        string WorkOrderNumber "Unique (WO-YYYYMM-XXXX)"
        int EquipmentId FK
        string MaintenanceType "Preventive / Corrective"
        string ProblemDescription
        string Status "Draft, Assigned, InProgress, WaitingParts, Completed, Closed"
        string AssignedMechanic
        datetime BreakdownStartTime
        datetime CompletedTime
    }

    SPARE_PARTS {
        int Id PK
        string PartNumber "Unique (e.g. 6732-71-6111)"
        string PartName
        int StockQuantity
        decimal UnitPrice
    }

    WORK_ORDER_PARTS {
        int Id PK
        int WorkOrderId FK
        int SparePartId FK
        int QuantityUsed
        decimal UnitPriceAtTime
    }
```

---

## 🔄 3. Sequence Diagram: Penyelesaian Work Order & Transaksi Suku Cadang (SIT-01)

Diagram alur transaksi atomik penyelesaian tiket perbaikan dan pemotongan inventaris:

```mermaid
sequenceDiagram
    autonumber
    actor ChiefMechanic as Chief Mechanic
    participant WebUI as Kendo UI / Frontend
    participant WOController as WorkOrdersController
    participant WOService as WorkOrderService
    participant DB as SQL Server (EF Core Transaction)

    ChiefMechanic->>WebUI: Klik "Selesaikan WO" & Pilih Suku Cadang
    WebUI->>WOController: PUT /api/v1/work-orders/{id}/complete
    WOController->>WOService: CompleteWorkOrderAsync(id, partsUsed)
    
    WOService->>DB: BeginTransactionAsync()
    WOService->>DB: Cek ketersediaan StockQuantity di gudang
    
    alt Stok Suku Cadang Cukup
        WOService->>DB: Kurangi StockQuantity suku cadang
        WOService->>DB: Catat ke tabel WorkOrderParts
        WOService->>DB: Set Status WO = 'Completed'
        WOService->>DB: CommitAsync()
        WOService-->>WOController: Return Success Response
        WOController-->>WebUI: 200 OK (Stok terpotong)
        WebUI-->>ChiefMechanic: Tampilkan notifikasi Berhasil
    else Stok Suku Cadang Kurang (Contoh: Minta 5 pcs, Tersedia 2 pcs)
        WOService->>DB: RollbackAsync() (Batalkan semua mutasi)
        WOService-->>WOController: Return Error (Stok tidak mencukupi)
        WOController-->>WebUI: 400 Bad Request
        WebUI-->>ChiefMechanic: Peringatan: Transaksi di-rollback aman
    end
```

---

## 🚀 4. Fitur Utama Sistem

1. **Master Armada & Telematika Hour Meter (HM):**
   - Validasi bisnis ketat: input angka HM baru tidak boleh lebih kecil dari catatan sebelumnya.
   - Peringatan Servis Otomatis (*Preventive Maintenance Alert*) ketika unit mendekati kelipatan 250, 500, 1000, atau 2000 jam kerja mesin.
2. **Siklus Hidup Work Order & Sinkronisasi Armada:**
   - Alur status ketat: `Draft` → `Assigned` → `InProgress` → `WaitingParts` → `Completed` → `Closed`.
   - Pembuatan WO jenis *Breakdown* otomatis mengubah status unit menjadi `Breakdown`. Saat WO ditutup (`Closed`), unit otomatis kembali berstatus `Operating`.
3. **Katalog Suku Cadang & Transaksi Atomik:**
   - Pemotongan stok berbasis transaksi ACID di SQL Server untuk memastikan tidak terjadi inkonsistensi data.
4. **Dashboard Eksekutif & Caching Redis:**
   - Menyajikan metrik **Physical Availability (PA)** dan daftar armada dengan frekuensi kerusakan tertinggi.
   - Hasil query otomatis di-cache ke **Redis** selama 5 menit untuk efisiensi beban query database.
5. **Dokumentasi Pengujian Teruji (SIT & UAT):**
   - Seluruh skenario pengujian integrasi (SIT-01, SIT-02) dan penerimaan pengguna (UAT-01, UAT-02) tersedia dan lulus 100% pada proyek uji `HEMS.Tests`. Rincian lengkap ada di [docs/TESTING_SIT_UAT.md](docs/TESTING_SIT_UAT.md).

---

## 🛠️ 5. Panduan Menjalankan Proyek

### Prasyarat
- **.NET 8 SDK**
- **Docker Desktop**

### Langkah 1: Jalankan Infrastruktur Database & Cache (Docker)
```powershell
docker compose up -d
```
Container `hems-sqlserver` (port 1433) dan `hems-redis` (port 6379) akan aktif.

### Langkah 2: Jalankan Automated Unit & Integration Tests
```powershell
dotnet test
```

### Langkah 3: Jalankan Aplikasi Web & API
```powershell
dotnet run --project src/HEMS.Web
```

Akses melalui browser:
- **Executive Fleet Dashboard:** `http://localhost:5236/`
- **Manajemen Work Order:** `http://localhost:5236/WorkOrders`
- **Katalog Suku Cadang:** `http://localhost:5236/SpareParts`
- **Interactive Swagger REST API:** `http://localhost:5236/swagger`
