# 🏢 Shanto Vila Management System
> A modern, full-featured Residential Property & Tenant Management Web Application built with **ASP.NET Core (.NET 9)**, **Entity Framework Core**, **MySQL / TiDB Cloud**, **QuestPDF**, and **Cloudinary**.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Database](https://img.shields.io/badge/Database-MySQL%20%7C%20TiDB-4479A1?logo=mysql&logoColor=white)](https://www.mysql.com/)
[![QuestPDF](https://img.shields.io/badge/PDF-QuestPDF_2026-critical)](https://www.questpdf.com/)
[![Cloudinary](https://img.shields.io/badge/Storage-Cloudinary-blue?logo=cloudinary&logoColor=white)](https://cloudinary.com/)
[![PWA](https://img.shields.io/badge/PWA-Ready-orange?logo=pwa&logoColor=white)](https://web.dev/progressive-web-apps/)

---

## 📖 Overview

**Shanto Vila Management System** is an end-to-end property management system designed to streamline residential rental operations for landlords and property managers. It eliminates manual bookkeeping by automating electricity sub-meter calculations, tenant record keeping, monthly bill generation, payment tracking, and receipt printing.

---

## ✨ Key Features

### 1. 🏢 Room & Unit Management
- **Occupancy Tracking:** Instant overview of Occupied vs. Vacant units.
- **Unit Configuration:** Custom base rent, designated sub-meter numbers, and custom utility charges.
- **Unit History:** Quick navigation to active tenants and billing records per room.

### 2. 👥 Resident & Tenant Profiling
- **Comprehensive Tenant Profiles:** Full name, phone numbers, profession, workplace, and permanent/present address.
- **Family Member Tracking:** Register secondary occupants under the primary tenant.
- **Document Management:** Cloud-stored tenant photos and National ID (NID) cards powered by Cloudinary with automatic local fallback.
- **Emergency Contacts:** Linked emergency contact directory for prompt communication.
- **Advance Deposit Ledger:** Track security deposits and advances.

### 3. ⚡ Smart Electricity Sub-Meter Billing
- **Sub-Meter Reading Input:** Enter previous and current meter readings per room.
- **Automated Usage Calculation:** Automatically computes consumed units and charges using dynamic per-unit electricity tariffs.
- **Bengali Digit Support:** Built-in Bengali numerals and currency formatting for clear tenant understanding.

### 4. 🧾 Monthly Bill Generation & PDF Invoices
- **Consolidated Monthly Bills:** Automatically computes `Base Rent + Electricity Bill + Custom/Other Utilities + Previous Unpaid Dues`.
- **Duplicate Prevention:** Safe unique indexing prevents accidental double billing for the same month and unit.
- **Printable Invoices:** High-resolution printable PDF invoices and receipts built with **QuestPDF**.

### 5. 💳 Payment Collection & Dues Management
- **Real-time Status:** Clear visual badges for `Paid`, `Unpaid`, and `Partial` payment states.
- **Ledger & History:** Detailed records of collection dates, amounts paid, and remaining arrears.

### 6. 📱 Progressive Web App (PWA) & Responsive UI
- **Mobile-First Design:** Fully responsive interface tailored for smartphones, tablets, and desktops.
- **Installable PWA:** Can be installed on Android, iOS, Windows, and macOS home screens.
- **Bilingual Interface:** Thoughtfully crafted Bengali and English UI components.

---

## 🛠️ Technology Stack

| Layer | Technology |
| :--- | :--- |
| **Framework** | [ASP.NET Core (.NET 9.0)](https://dotnet.microsoft.com/) MVC |
| **Language** | C# 13 |
| **ORM** | [Entity Framework Core 9.0](https://learn.microsoft.com/en-us/ef/core/) (Pomelo MySQL) |
| **Database** | MySQL 8.0+ / TiDB Cloud Serverless |
| **Document Generation** | [QuestPDF 2026](https://www.questpdf.com/) |
| **Media Cloud Storage** | [Cloudinary](https://cloudinary.com/) (with local disk fallback) |
| **Frontend** | HTML5, CSS3, JavaScript, Bootstrap Icons |
| **PWA** | Service Worker (`sw.js`) & Web App Manifest |
| **Containerization** | Docker (`mcr.microsoft.com/dotnet/aspnet:9.0`) |

---

## 🏗️ Architecture & Project Structure

```
Shanto-Vila-Management-System/
├── Controllers/              # MVC Controllers (Dashboard, Residents, Bills, Electricity, etc.)
├── Data/                     # DbContext and DbInitializer (Auto-migrations & sample seeds)
├── Helpers/                  # Bangla language helpers, Number formatting, File helpers
├── Models/                   # EF Core Data Entities (Room, Resident, MonthlyBill, Setting, etc.)
├── Services/                 # Business services (PdfService, CloudStorageService, BillService)
├── ViewModels/               # Strongly-typed Views & DTO models
├── Views/                    # Razor View templates
├── wwwroot/                  # Static assets (CSS, JS, Fonts, Icons, Manifest, Service Worker)
├── Dockerfile                # Multi-stage Docker build configuration
├── appsettings.Example.json  # Configuration template for environment setup
└── Program.cs                # Application bootstrap & dependency injection
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MySQL Server](https://dev.mysql.com/downloads/mysql/) (or XAMPP / TiDB Cloud)
- (Optional) Free [Cloudinary Account](https://cloudinary.com/) for cloud photo storage

### 1. Clone the Repository
```bash
git clone https://github.com/mdsaniislamshanto/House-Rental-Management-System.git
cd House-Rental-Management-System
```

### 2. Configure Environment & Database
Copy `appsettings.Example.json` to `appsettings.json`:
```bash
cp "Shanto VilaManagement System/appsettings.Example.json" "Shanto VilaManagement System/appsettings.json"
```

Update your database connection string and optional Cloudinary credentials:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=shanto_vila_db;User Id=root;Password=your_password;CharSet=utf8mb4;"
  },
  "Cloudinary": {
    "CloudName": "YOUR_CLOUDINARY_CLOUD_NAME",
    "ApiKey": "YOUR_CLOUDINARY_API_KEY",
    "ApiSecret": "YOUR_CLOUDINARY_API_SECRET"
  }
}
```

> **Note:** If Cloudinary is not configured, image uploads will automatically fall back to local disk storage (`wwwroot/uploads`).

### 3. Run the Application
```bash
dotnet restore
dotnet run --project "Shanto VilaManagement System"
```
Navigate to `https://localhost:5001` or `http://localhost:5000` in your web browser. The database schema and initial sample seed data will be created automatically on first run!

---

## 🐳 Running with Docker

Build and run the container locally:
```bash
docker build -t shanto-vila-app .
docker run -p 8080:8080 -e "ConnectionStrings__DefaultConnection=your_connection_string" shanto-vila-app
```
Access the application at `http://localhost:8080`.

---

## 🔒 Security Best Practices
- **No hardcoded secrets:** All production credentials, database connection strings, and API secrets are loaded through environment variables or local `appsettings.json` (excluded from git).
- **Safe Fallbacks:** Offline/local fallbacks are implemented for both database connections and media file handling.

---

## 👤 Author

Developed by **Sany Islam Shanto**
- GitHub: [@mdsaniislamshanto](https://github.com/mdsaniislamshanto)
- Email: [mdsaniislamshanto@gmail.com](mailto:mdsaniislamshanto@gmail.com)

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).
