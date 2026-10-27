# PRODUCT REQUIREMENTS DOCUMENT (PRD)

- **Nama Proyek:** Heavy Equipment Maintenance & Fleet Management System (HEMS)
- **Versi Dokumen:** 1.0
- **Target Ekosistem:** Enterprise Back-Office (Studi Kasus Operasional Alat Berat)
- **Tech Stack Utama:** C#, .NET 8 (LTS - penerus .NET 5), Microsoft SQL Server, jQuery, Telerik Kendo UI

---

## 1. Tujuan Bisnis & Latar Belakang (Business Objectives)

Perusahaan pengelola armada alat berat membutuhkan sistem terpusat untuk memantau jam operasional mesin (*Hour Meter*), menjadwalkan perawatan berkala (*Preventive Maintenance*), mencatat perbaikan darurat (*Corrective Maintenance*), dan mengontrol pemakaian suku cadang di berbagai lokasi tambang (*Job Site*).

### Indikator Keberhasilan Sistem (KPI)
- **Otomatisasi Jadwal Servis:** Mencegah keterlambatan servis berkala setiap kelipatan 250, 500, 1000, dan 2000 *Hour Meter* (HM).
- **Transparansi Suku Cadang:** Memangkas waktu pencarian nomor suku cadang (*Part Number*) dan memvalidasi stok gudang secara *real-time* saat pembuatan tiket perbaikan.
- **Visibilitas Kinerja Armada:** Menyajikan metrik *Physical Availability* (PA) dan *Mean Time Between Failures* (MTBF) secara akurat di dashboard manajemen.

---

## 2. Peran Pengguna & Akses (Role-Based Access Control)

Autentikasi dirancang mendukung integrasi **Keycloak** (*Single Sign-On* / OAuth2 & OpenID Connect) atau **JWT Bearer Token** dengan pembagian peran berikut:

| Peran (Role) | Tanggung Jawab Utama di Sistem | Hak Akses Modul |
| :--- | :--- | :--- |
| **Equipment Planner** | Memantau HM harian unit, merencanakan jadwal servis berkala, dan menerbitkan *Work Order* (WO). | Master Unit, Log HM, Pembuatan & Penugasan WO. |
| **Chief Mechanic** | Menerima WO, menugaskan mekanik lapangan, mencatat pemakaian suku cadang, dan menutup WO. | Update Status WO, Permintaan Suku Cadang, Riwayat Servis. |
| **Warehouse Admin** | Mengelola stok suku cadang dan menyetujui pengeluaran barang untuk kebutuhan WO. | Katalog Suku Cadang, Mutasi Stok, Integrasi Pencarian. |
| **Plant Manager** | Memantau performa kesiapan alat berat dan biaya pemeliharaan lintas lokasi tambang. | Dashboard Eksekutif, Laporan Analitik (*Read-Only*). |

---

## 3. Spesifikasi Kebutuhan Fungsional (Functional Requirements)

### Modul 1: Manajemen Master Armada & Telematika Hour Meter (HM)
- **FR-1.1:** Sistem dapat melakukan operasi CRUD data unit alat berat (Kode Unit, Serial Number, Brand, Model, Tahun Perakitan, Lokasi Site, dan Status Operasional: `Operating`, `Standby`, `Breakdown`, `Under Maintenance`).
- **FR-1.2:** Sistem menyediakan fitur pencatatan *Daily Hour Meter (HM) Log* (baik input manual maupun endpoint API simulasi sensor IoT/telematika).
- **FR-1.3:** Validasi bisnis wajib menolak input angka HM baru yang nilainya lebih kecil dari angka HM terakhir unit tersebut.
- **FR-1.4:** Ketika selisih HM sejak servis terakhir mendekati batas ambang servis berkala (misal kurang dari 25 jam menuju kelipatan 250 HM), sistem otomatis menghasilkan peringatan *Preventive Maintenance Alert*.

### Modul 2: Manajemen Work Order (Tiket Perbaikan & Servis)
- **FR-2.1:** Pembuatan *Work Order* (WO) dibagi menjadi dua kategori: *Preventive Maintenance* (terjadwal berdasarkan HM) dan *Corrective Maintenance* (insidental saat alat mengalami *breakdown*).
- **FR-2.2:** Siklus hidup status WO mengikuti alur ketat (*State Machine*): `Draft` → `Assigned` → `In Progress` → `Waiting Parts` → `Completed` → `Closed`.
- **FR-2.3:** Saat WO berjenis *Breakdown* dibuat, status unit alat berat otomatis berubah menjadi `Breakdown`. Saat WO diubah menjadi `Closed`, status unit otomatis kembali menjadi `Operating`.
- **FR-2.4:** Pencatatan waktu mulai dan selesai pengerjaan (*Downtime Tracking*) untuk kalkulasi durasi perbaikan.

### Modul 3: Katalog & Konsumsi Suku Cadang (Spare Parts Management)
- **FR-3.1:** Pengelolaan master suku cadang yang mencakup *Part Number*, Nama Komponen, Kategori, Kompatibilitas Model Alat, Stok Tersedia, dan Harga Satuan.
- **FR-3.2:** Penambahan daftar suku cadang yang digunakan ke dalam rincian *Work Order* (*Work Order Items*).
- **FR-3.3:** Saat status WO diubah menjadi `Completed`, sistem mengeksekusi transaksi database (*Database Transaction*) untuk memotong stok suku cadang di gudang dan mencatat riwayat mutasi barang secara atomik.

### Modul 4: Dashboard Operasional & Pelaporan Enterprise
- **FR-4.1:** Menampilkan tabel interaktif menggunakan **Telerik Kendo UI Grid** dengan fitur *server-side paging*, *sorting*, *multi-column filtering*, dan ekspor langsung ke Excel/PDF.
- **FR-4.2:** Menampilkan metrik ketersediaan unit (persentase unit `Operating` dibanding total populasi) serta daftar 5 unit dengan frekuensi kerusakan tertinggi.

---

## 4. Spesifikasi Teknis & Arsitektur (Pemetaan Syarat Lowongan Kerja)

Bagian ini dirancang khusus agar setiap poin persyaratan teknis pada lowongan kerja terimplementasi nyata di dalam kode portofolio:

| Persyaratan di Lowongan | Implementasi di Proyek HEMS |
| :--- | :--- |
| **C# & .NET 5+** | Menggunakan ASP.NET Core (.NET 8 LTS) dengan pemisahan layer *Clean Architecture* (`HEMS.Domain`, `HEMS.Application`, `HEMS.Infrastructure`, `HEMS.WebAPI`). |
| **Pengembangan & Integrasi API** | Menyediakan RESTful API berstandar OpenAPI/Swagger, mendukung konsumsi oleh Kendo UI (AJAX) serta endpoint khusus simulasi penerima data HM dari alat berat. |
| **Microsoft SQL Server (DDL, DML, Query)** | Menggunakan EF Core untuk operasi ORM standar, dilengkapi Raw SQL / Stored Procedure untuk kalkulasi metrik laporan bulanan (MTBF & PA) guna membuktikan penguasaan T-SQL. |
| **JavaScript, jQuery, Telerik Kendo UI** | Antarmuka web dibangun menggunakan Razor Views yang memanggil API via jQuery AJAX dan merender tabel data kompleks menggunakan komponen Kendo UI Grid & Kendo DropDownList. |
| **Keycloak (Nilai Tambah)** | Mengintegrasikan autentikasi berbasis Docker image Keycloak lokal menggunakan middleware `AddJwtBearer` di .NET, atau simulasi *Role-Based JWT Auth*. |
| **Redis (Nilai Tambah)** | Menyimpan *cache* untuk data *Master Equipment* dan ringkasan statistik Dashboard selama 5 menit menggunakan `IDistributedCache` agar database tidak terbebani query berulang. |
| **Elasticsearch (Nilai Tambah)** | Digunakan pada fitur pencarian cepat katalog suku cadang (*Part Number* & nama alternatif komponen) dengan kemampuan *fuzzy search*. |
| **Docker / Kubernetes (Nilai Tambah)** | Menyediakan file `docker-compose.yml` untuk menjalankan aplikasi .NET, SQL Server, Redis, dan Keycloak dalam satu perintah, ditambah file manifest `deployment.yaml` sederhana untuk Kubernetes. |

---

## 5. Rancangan Skema Database Inti (SQL Server DDL Blueprint)

Berikut adalah desain struktur tabel relasional di Microsoft SQL Server:

### 1. `Equipments` (Master Unit Alat Berat)
- `Id` (`PK, INT IDENTITY`)
- `EquipmentCode` (`VARCHAR(20)`, Unique, Indexed — misal: `EX-2001`)
- `SerialNumber` (`VARCHAR(50)`, Unique)
- `Brand` (`VARCHAR(50)`) & `Model` (`VARCHAR(50)` — misal: `Komatsu PC200-8`)
- `CurrentHourMeter` (`DECIMAL(10,2)`)
- `LastServiceHourMeter` (`DECIMAL(10,2)`)
- `Status` (`VARCHAR(20)` — `Operating`, `Breakdown`, `Maintenance`)
- `SiteLocation` (`VARCHAR(50)`)

### 2. `HourMeterLogs` (Riwayat Jam Kerja Mesin)
- `Id` (`PK, BIGINT IDENTITY`)
- `EquipmentId` (`FK` ke `Equipments.Id`)
- `RecordedDate` (`DATETIME2`)
- `PreviousHM` (`DECIMAL(10,2)`) & `CurrentHM` (`DECIMAL(10,2)`)
- `RecordedBy` (`VARCHAR(50)`)

### 3. `WorkOrders` (Tiket Servis & Perbaikan)
- `Id` (`PK, INT IDENTITY`)
- `WorkOrderNumber` (`VARCHAR(30)`, Unique — misal: `WO-202610-0001`)
- `EquipmentId` (`FK` ke `Equipments.Id`)
- `MaintenanceType` (`VARCHAR(20)` — `Preventive` / `Corrective`)
- `ProblemDescription` (`NVARCHAR(500)`)
- `Status` (`VARCHAR(20)`)
- `AssignedMechanic` (`VARCHAR(100)`)
- `BreakdownStartTime` (`DATETIME2`) & `CompletedTime` (`DATETIME2`, Nullable)

### 4. `SpareParts` (Master Suku Cadang)
- `Id` (`PK, INT IDENTITY`)
- `PartNumber` (`VARCHAR(50)`, Unique, Indexed)
- `PartName` (`NVARCHAR(150)`)
- `StockQuantity` (`INT`)
- `UnitPrice` (`DECIMAL(18,2)`)

### 5. `WorkOrderParts` (Relasi Pemakaian Suku Cadang pada WO)
- `Id` (`PK, INT IDENTITY`)
- `WorkOrderId` (`FK` ke `WorkOrders.Id`)
- `SparePartId` (`FK` ke `SpareParts.Id`)
- `QuantityUsed` (`INT`)
- `UnitPriceAtTime` (`DECIMAL(18,2)`)

---

## 6. Spesifikasi Endpoint API Utama (RESTful Contract)

| Method | Endpoint URI | Deskripsi Fungsi | Fitur Teknis yang Ditonjolkan |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/equipments` | Mengambil daftar unit dengan paginasi, filter lokasi, dan sorting. | Mendukung parameter query dari Kendo UI Grid. |
| `POST` | `/api/v1/equipments/{id}/hm-logs` | Mencatat kenaikan Hour Meter harian dan mengecek ambang batas servis. | Validasi bisnis & trigger otomatis status *Preventive Alert*. |
| `POST` | `/api/v1/work-orders` | Membuat tiket Work Order baru dan mengubah status alat berat. | Implementasi DTO Validation dan Database Transaction. |
| `PUT` | `/api/v1/work-orders/{id}/complete` | Menyelesaikan WO, memotong stok suku cadang, dan mengembalikan status unit ke `Operating`. | Transaksi atomik (`BeginTransactionAsync` / `CommitAsync`). |
| `GET` | `/api/v1/spare-parts/search` | Mencari suku cadang berdasarkan kode atau nama komponen. | Integrasi pencarian cepat via Elasticsearch atau SQL Full-Text Index. |
| `GET` | `/api/v1/dashboard/summary` | Mengambil ringkasan PA, MTBF, dan jumlah WO aktif. | Eksekusi Stored Procedure SQL Server yang di-cache ke Redis. |

---

## 7. Rencana Pengujian & Keamanan (SIT, UAT, & Security Testing)

Untuk memenuhi syarat *"Berpengalaman dalam SIT, UAT, dan mendukung pengujian keamanan aplikasi"*, sertakan dokumen pengujian berikut di dalam repositori proyek:

### A. System Integration Testing (SIT)
Menguji integrasi antar-modul dan antar-sistem secara menyeluruh:
- **SIT-01 (API ke Database Transaction):** Memastikan saat proses pemotongan stok suku cadang gagal (misal stok kurang dari jumlah yang diminta pada WO), status Work Order batal berubah menjadi `Completed` (*Rollback* berjalan sempurna).
- **SIT-02 (API ke Redis Cache):** Memastikan saat status alat berat berubah menjadi `Breakdown`, cache ringkasan dashboard di Redis langsung dihapus (*Cache Invalidation*) sehingga data terbaru langsung tampil.

### B. User Acceptance Testing (UAT)
Daftar skenario pengujian dari sudut pandang alur kerja bisnis pengguna:
- **UAT-01:** *Equipment Planner* menginput HM unit Komatsu PC200 dari 240 HM menjadi 252 HM → Sistem menampilkan notifikasi bahwa unit wajib menjalani *Preventive Maintenance* 250 HM.
- **UAT-02:** *Chief Mechanic* menambahkan 2 buah *Hydraulic Filter* ke dalam WO dan menyelesaikan pekerjaan → Stok gudang berkurang tepat 2 buah dan status unit kembali `Operating`.

### C. Pengujian Keamanan Aplikasi (Penetration Testing Support)
- **Pencegahan SQL Injection:** Seluruh parameter pencarian dan filter pada Kendo UI Grid maupun Stored Procedure diproses menggunakan *Parameterized Queries* di Entity Framework Core.
- **Broken Access Control (OWASP A01):** Menguji endpoint `PUT /api/v1/work-orders/{id}/complete` menggunakan token akun tanpa role *Chief Mechanic* dan memastikan API mengembalikan status `403 Forbidden`.
- **Validasi Input & Rate Limiting:** Membatasi ukuran payload JSON dan menerapkan sanitasi input untuk mencegah *Stored XSS* pada kolom deskripsi kerusakan.

---

## 8. Daftar Dokumentasi Teknis untuk Portofolio GitHub

Agar portofolio ini langsung memikat tim rekrutmen teknis PT Global Service Indonesia / UT Group, lengkapi repositori Git dengan artefak berikut di dalam file `README.md`:

1. **System Architecture Diagram:** Gambar alur komunikasi antara Client (jQuery/Kendo UI) → ASP.NET Core Web API → Keycloak, Redis, Elasticsearch, dan SQL Server.
2. **Entity Relationship Diagram (ERD):** Visualisasi relasi ke-5 tabel utama di atas.
3. **Sequence Diagram:** Alur proses penyelesaian Work Order hingga pemotongan stok suku cadang.
4. **Dokumen Tabel SIT & UAT:** Lampirkan tabel skenario uji beserta status *Pass/Fail* di dalam folder `/docs` pada repositori.
# PRODUCT REQUIREMENTS DOCUMENT (PRD)

- **Nama Proyek:** Heavy Equipment Maintenance & Fleet Management System (HEMS)
- **Versi Dokumen:** 1.0
- **Target Ekosistem:** Enterprise Back-Office (Studi Kasus Operasional Alat Berat)
- **Tech Stack Utama:** C#, .NET 8 (LTS - penerus .NET 5), Microsoft SQL Server, jQuery, Telerik Kendo UI

---

## 1. Tujuan Bisnis & Latar Belakang (Business Objectives)

Perusahaan pengelola armada alat berat membutuhkan sistem terpusat untuk memantau jam operasional mesin (*Hour Meter*), menjadwalkan perawatan berkala (*Preventive Maintenance*), mencatat perbaikan darurat (*Corrective Maintenance*), dan mengontrol pemakaian suku cadang di berbagai lokasi tambang (*Job Site*).

### Indikator Keberhasilan Sistem (KPI)
- **Otomatisasi Jadwal Servis:** Mencegah keterlambatan servis berkala setiap kelipatan 250, 500, 1000, dan 2000 *Hour Meter* (HM).
- **Transparansi Suku Cadang:** Memangkas waktu pencarian nomor suku cadang (*Part Number*) dan memvalidasi stok gudang secara *real-time* saat pembuatan tiket perbaikan.
- **Visibilitas Kinerja Armada:** Menyajikan metrik *Physical Availability* (PA) dan *Mean Time Between Failures* (MTBF) secara akurat di dashboard manajemen.

---

## 2. Peran Pengguna & Akses (Role-Based Access Control)

Autentikasi dirancang mendukung integrasi **Keycloak** (*Single Sign-On* / OAuth2 & OpenID Connect) atau **JWT Bearer Token** dengan pembagian peran berikut:

| Peran (Role) | Tanggung Jawab Utama di Sistem | Hak Akses Modul |
| :--- | :--- | :--- |
| **Equipment Planner** | Memantau HM harian unit, merencanakan jadwal servis berkala, dan menerbitkan *Work Order* (WO). | Master Unit, Log HM, Pembuatan & Penugasan WO. |
| **Chief Mechanic** | Menerima WO, menugaskan mekanik lapangan, mencatat pemakaian suku cadang, dan menutup WO. | Update Status WO, Permintaan Suku Cadang, Riwayat Servis. |
| **Warehouse Admin** | Mengelola stok suku cadang dan menyetujui pengeluaran barang untuk kebutuhan WO. | Katalog Suku Cadang, Mutasi Stok, Integrasi Pencarian. |
| **Plant Manager** | Memantau performa kesiapan alat berat dan biaya pemeliharaan lintas lokasi tambang. | Dashboard Eksekutif, Laporan Analitik (*Read-Only*). |

---

## 3. Spesifikasi Kebutuhan Fungsional (Functional Requirements)

### Modul 1: Manajemen Master Armada & Telematika Hour Meter (HM)
- **FR-1.1:** Sistem dapat melakukan operasi CRUD data unit alat berat (Kode Unit, Serial Number, Brand, Model, Tahun Perakitan, Lokasi Site, dan Status Operasional: `Operating`, `Standby`, `Breakdown`, `Under Maintenance`).
- **FR-1.2:** Sistem menyediakan fitur pencatatan *Daily Hour Meter (HM) Log* (baik input manual maupun endpoint API simulasi sensor IoT/telematika).
- **FR-1.3:** Validasi bisnis wajib menolak input angka HM baru yang nilainya lebih kecil dari angka HM terakhir unit tersebut.
- **FR-1.4:** Ketika selisih HM sejak servis terakhir mendekati batas ambang servis berkala (misal kurang dari 25 jam menuju kelipatan 250 HM), sistem otomatis menghasilkan peringatan *Preventive Maintenance Alert*.

### Modul 2: Manajemen Work Order (Tiket Perbaikan & Servis)
- **FR-2.1:** Pembuatan *Work Order* (WO) dibagi menjadi dua kategori: *Preventive Maintenance* (terjadwal berdasarkan HM) dan *Corrective Maintenance* (insidental saat alat mengalami *breakdown*).
- **FR-2.2:** Siklus hidup status WO mengikuti alur ketat (*State Machine*): `Draft` → `Assigned` → `In Progress` → `Waiting Parts` → `Completed` → `Closed`.
- **FR-2.3:** Saat WO berjenis *Breakdown* dibuat, status unit alat berat otomatis berubah menjadi `Breakdown`. Saat WO diubah menjadi `Closed`, status unit otomatis kembali menjadi `Operating`.
- **FR-2.4:** Pencatatan waktu mulai dan selesai pengerjaan (*Downtime Tracking*) untuk kalkulasi durasi perbaikan.

### Modul 3: Katalog & Konsumsi Suku Cadang (Spare Parts Management)
- **FR-3.1:** Pengelolaan master suku cadang yang mencakup *Part Number*, Nama Komponen, Kategori, Kompatibilitas Model Alat, Stok Tersedia, dan Harga Satuan.
- **FR-3.2:** Penambahan daftar suku cadang yang digunakan ke dalam rincian *Work Order* (*Work Order Items*).
- **FR-3.3:** Saat status WO diubah menjadi `Completed`, sistem mengeksekusi transaksi database (*Database Transaction*) untuk memotong stok suku cadang di gudang dan mencatat riwayat mutasi barang secara atomik.

### Modul 4: Dashboard Operasional & Pelaporan Enterprise
- **FR-4.1:** Menampilkan tabel interaktif menggunakan **Telerik Kendo UI Grid** dengan fitur *server-side paging*, *sorting*, *multi-column filtering*, dan ekspor langsung ke Excel/PDF.
- **FR-4.2:** Menampilkan metrik ketersediaan unit (persentase unit `Operating` dibanding total populasi) serta daftar 5 unit dengan frekuensi kerusakan tertinggi.

---

## 4. Spesifikasi Teknis & Arsitektur (Pemetaan Syarat Lowongan Kerja)

Bagian ini dirancang khusus agar setiap poin persyaratan teknis pada lowongan kerja terimplementasi nyata di dalam kode portofolio:

| Persyaratan di Lowongan | Implementasi di Proyek HEMS |
| :--- | :--- |
| **C# & .NET 5+** | Menggunakan ASP.NET Core (.NET 8 LTS) dengan pemisahan layer *Clean Architecture* (`HEMS.Domain`, `HEMS.Application`, `HEMS.Infrastructure`, `HEMS.WebAPI`). |
| **Pengembangan & Integrasi API** | Menyediakan RESTful API berstandar OpenAPI/Swagger, mendukung konsumsi oleh Kendo UI (AJAX) serta endpoint khusus simulasi penerima data HM dari alat berat. |
| **Microsoft SQL Server (DDL, DML, Query)** | Menggunakan EF Core untuk operasi ORM standar, dilengkapi Raw SQL / Stored Procedure untuk kalkulasi metrik laporan bulanan (MTBF & PA) guna membuktikan penguasaan T-SQL. |
| **JavaScript, jQuery, Telerik Kendo UI** | Antarmuka web dibangun menggunakan Razor Views yang memanggil API via jQuery AJAX dan merender tabel data kompleks menggunakan komponen Kendo UI Grid & Kendo DropDownList. |
| **Keycloak (Nilai Tambah)** | Mengintegrasikan autentikasi berbasis Docker image Keycloak lokal menggunakan middleware `AddJwtBearer` di .NET, atau simulasi *Role-Based JWT Auth*. |
| **Redis (Nilai Tambah)** | Menyimpan *cache* untuk data *Master Equipment* dan ringkasan statistik Dashboard selama 5 menit menggunakan `IDistributedCache` agar database tidak terbebani query berulang. |
| **Elasticsearch (Nilai Tambah)** | Digunakan pada fitur pencarian cepat katalog suku cadang (*Part Number* & nama alternatif komponen) dengan kemampuan *fuzzy search*. |
| **Docker / Kubernetes (Nilai Tambah)** | Menyediakan file `docker-compose.yml` untuk menjalankan aplikasi .NET, SQL Server, Redis, dan Keycloak dalam satu perintah, ditambah file manifest `deployment.yaml` sederhana untuk Kubernetes. |

---

## 5. Rancangan Skema Database Inti (SQL Server DDL Blueprint)

Berikut adalah desain struktur tabel relasional di Microsoft SQL Server:

### 1. `Equipments` (Master Unit Alat Berat)
- `Id` (`PK, INT IDENTITY`)
- `EquipmentCode` (`VARCHAR(20)`, Unique, Indexed — misal: `EX-2001`)
- `SerialNumber` (`VARCHAR(50)`, Unique)
- `Brand` (`VARCHAR(50)`) & `Model` (`VARCHAR(50)` — misal: `Komatsu PC200-8`)
- `CurrentHourMeter` (`DECIMAL(10,2)`)
- `LastServiceHourMeter` (`DECIMAL(10,2)`)
- `Status` (`VARCHAR(20)` — `Operating`, `Breakdown`, `Maintenance`)
- `SiteLocation` (`VARCHAR(50)`)

### 2. `HourMeterLogs` (Riwayat Jam Kerja Mesin)
- `Id` (`PK, BIGINT IDENTITY`)
- `EquipmentId` (`FK` ke `Equipments.Id`)
- `RecordedDate` (`DATETIME2`)
- `PreviousHM` (`DECIMAL(10,2)`) & `CurrentHM` (`DECIMAL(10,2)`)
- `RecordedBy` (`VARCHAR(50)`)

### 3. `WorkOrders` (Tiket Servis & Perbaikan)
- `Id` (`PK, INT IDENTITY`)
- `WorkOrderNumber` (`VARCHAR(30)`, Unique — misal: `WO-202610-0001`)
- `EquipmentId` (`FK` ke `Equipments.Id`)
- `MaintenanceType` (`VARCHAR(20)` — `Preventive` / `Corrective`)
- `ProblemDescription` (`NVARCHAR(500)`)
- `Status` (`VARCHAR(20)`)
- `AssignedMechanic` (`VARCHAR(100)`)
- `BreakdownStartTime` (`DATETIME2`) & `CompletedTime` (`DATETIME2`, Nullable)

### 4. `SpareParts` (Master Suku Cadang)
- `Id` (`PK, INT IDENTITY`)
- `PartNumber` (`VARCHAR(50)`, Unique, Indexed)
- `PartName` (`NVARCHAR(150)`)
- `StockQuantity` (`INT`)
- `UnitPrice` (`DECIMAL(18,2)`)

### 5. `WorkOrderParts` (Relasi Pemakaian Suku Cadang pada WO)
- `Id` (`PK, INT IDENTITY`)
- `WorkOrderId` (`FK` ke `WorkOrders.Id`)
- `SparePartId` (`FK` ke `SpareParts.Id`)
- `QuantityUsed` (`INT`)
- `UnitPriceAtTime` (`DECIMAL(18,2)`)

---

## 6. Spesifikasi Endpoint API Utama (RESTful Contract)

| Method | Endpoint URI | Deskripsi Fungsi | Fitur Teknis yang Ditonjolkan |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/equipments` | Mengambil daftar unit dengan paginasi, filter lokasi, dan sorting. | Mendukung parameter query dari Kendo UI Grid. |
| `POST` | `/api/v1/equipments/{id}/hm-logs` | Mencatat kenaikan Hour Meter harian dan mengecek ambang batas servis. | Validasi bisnis & trigger otomatis status *Preventive Alert*. |
| `POST` | `/api/v1/work-orders` | Membuat tiket Work Order baru dan mengubah status alat berat. | Implementasi DTO Validation dan Database Transaction. |
| `PUT` | `/api/v1/work-orders/{id}/complete` | Menyelesaikan WO, memotong stok suku cadang, dan mengembalikan status unit ke `Operating`. | Transaksi atomik (`BeginTransactionAsync` / `CommitAsync`). |
| `GET` | `/api/v1/spare-parts/search` | Mencari suku cadang berdasarkan kode atau nama komponen. | Integrasi pencarian cepat via Elasticsearch atau SQL Full-Text Index. |
| `GET` | `/api/v1/dashboard/summary` | Mengambil ringkasan PA, MTBF, dan jumlah WO aktif. | Eksekusi Stored Procedure SQL Server yang di-cache ke Redis. |

---

## 7. Rencana Pengujian & Keamanan (SIT, UAT, & Security Testing)

Untuk memenuhi syarat *"Berpengalaman dalam SIT, UAT, dan mendukung pengujian keamanan aplikasi"*, sertakan dokumen pengujian berikut di dalam repositori proyek:

### A. System Integration Testing (SIT)
Menguji integrasi antar-modul dan antar-sistem secara menyeluruh:
- **SIT-01 (API ke Database Transaction):** Memastikan saat proses pemotongan stok suku cadang gagal (misal stok kurang dari jumlah yang diminta pada WO), status Work Order batal berubah menjadi `Completed` (*Rollback* berjalan sempurna).
- **SIT-02 (API ke Redis Cache):** Memastikan saat status alat berat berubah menjadi `Breakdown`, cache ringkasan dashboard di Redis langsung dihapus (*Cache Invalidation*) sehingga data terbaru langsung tampil.

### B. User Acceptance Testing (UAT)
Daftar skenario pengujian dari sudut pandang alur kerja bisnis pengguna:
- **UAT-01:** *Equipment Planner* menginput HM unit Komatsu PC200 dari 240 HM menjadi 252 HM → Sistem menampilkan notifikasi bahwa unit wajib menjalani *Preventive Maintenance* 250 HM.
- **UAT-02:** *Chief Mechanic* menambahkan 2 buah *Hydraulic Filter* ke dalam WO dan menyelesaikan pekerjaan → Stok gudang berkurang tepat 2 buah dan status unit kembali `Operating`.

### C. Pengujian Keamanan Aplikasi (Penetration Testing Support)
- **Pencegahan SQL Injection:** Seluruh parameter pencarian dan filter pada Kendo UI Grid maupun Stored Procedure diproses menggunakan *Parameterized Queries* di Entity Framework Core.
- **Broken Access Control (OWASP A01):** Menguji endpoint `PUT /api/v1/work-orders/{id}/complete` menggunakan token akun tanpa role *Chief Mechanic* dan memastikan API mengembalikan status `403 Forbidden`.
- **Validasi Input & Rate Limiting:** Membatasi ukuran payload JSON dan menerapkan sanitasi input untuk mencegah *Stored XSS* pada kolom deskripsi kerusakan.

---

## 8. Daftar Dokumentasi Teknis untuk Portofolio GitHub

Agar portofolio ini langsung memikat tim rekrutmen teknis PT Global Service Indonesia / UT Group, lengkapi repositori Git dengan artefak berikut di dalam file `README.md`:

1. **System Architecture Diagram:** Gambar alur komunikasi antara Client (jQuery/Kendo UI) → ASP.NET Core Web API → Keycloak, Redis, Elasticsearch, dan SQL Server.
2. **Entity Relationship Diagram (ERD):** Visualisasi relasi ke-5 tabel utama di atas.
3. **Sequence Diagram:** Alur proses penyelesaian Work Order hingga pemotongan stok suku cadang.
4. **Dokumen Tabel SIT & UAT:** Lampirkan tabel skenario uji beserta status *Pass/Fail* di dalam folder `/docs` pada repositori.
