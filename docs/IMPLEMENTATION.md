# سَلِّفني — SalefnyMVC implementation

Arabic-first peer-to-peer rental marketplace built as an ASP.NET Core 10 MVC application with SQL Server, ASP.NET Core Identity, escrow-style deposit tracking, and an admin back office.

> Demo/educational project. Escrow operations are a database ledger and status workflow; no real payment provider is integrated. Never deploy the seeded accounts or the OTP demo fallback to a public environment.

## Features

- Browse and search listings with category/city/price filters, sorting, paging and Arabic RTL views.
- Register/login with ASP.NET Core Identity; login is protected by a 6-digit email OTP.
- Four roles: `Renter`, `Owner`, `Admin`, `SuperAdmin`. New registrations start as `Renter`; owners are granted after an owner request is reviewed.
- Owner requests, six-angle product submission, and admin listing approval workflow.
- Booking checkout with inclusive rental-day rules, overlap checks, and a 1–30 day window.
- Escrow deposit lifecycle: hold / release / freeze / deduct / refund with an auditable ledger.
- Handover and return inspections, damage reports, disputes and admin resolution.
- Reviews and trust/average rating recalculation.
- In-app notifications plus optional email delivery.
- Identity verification with camera capture, OCR helper, 14-digit national ID validation and admin approval queue.
- Admin back office with DB-driven dashboard, users, categories, verifications, disputes, escrow ledger and CSV export.
- Public profiles, profile editing, password recovery and email confirmation links.
- Full-site light/dark theme toggle.
- xUnit + Moq unit tests and an end-to-end smoke script.

## Tech stack

| Area | Technology |
| --- | --- |
| Web | ASP.NET Core 10 MVC, Razor views, Tag Helpers |
| Data | EF Core 10, SQL Server, LINQ, migrations |
| Identity | ASP.NET Core Identity (roles, claims, password policy) |
| Messaging | SMTP email sender, Egyptian SMS gateway HTTP client |
| Testing | xUnit, Moq, Microsoft.NET.Test.Sdk, targeting `net10.0` |

## Local setup

1. Prerequisites: .NET 10 SDK, SQL Server / SQL Server Express, Visual Studio 2026 (or VS Code with C#).
2. The checked-in `appsettings.json` uses a generic local SQL Express connection string. Put your machine-specific connection string in `appsettings.Development.json` (git-ignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=SalefnyDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

3. Create/update the database:

```powershell
dotnet ef database update
```

4. Run with the `http` launch profile — it serves `http://localhost:5013`:

```powershell
dotnet run --launch-profile http
```

5. To enable real OTP delivery, fill the `Email` and `SmsGateway` sections in your local settings.

### Seeded credentials (development only)

| Account | Password | Roles |
| --- | --- | --- |
| `salefnyapplication@gmail.com` | `AdminSalefny@2026` | Renter, Admin |
| `salefny.app+superadmin@gmail.com` | `Super@2026!` | Renter, Admin, SuperAdmin |

### OTP demo fallback

Login sends a 6-digit code by email. If SMTP is not configured, the code is written to the application log and displayed on the OTP page as an explicitly labelled local demo fallback. This is for offline development only; configure SMTP before any deployment.

## Migrations

`InitialCreate`, `WidenVerificationDocumentNumber`, `SeedCategories`, `SeedShowcaseItems`, `BookingCheckoutAndInspections`, `OwnerRequestsAndRoles`, `ListingApproval`, `RatingsAndVerificationScan`, `OwnerRequestProductSubmission`, `FullSpecCompletion`.

## Tests

```powershell
dotnet test Tests/SalefnyMVC.Tests/SalefnyMVC.Tests.csproj
```

End-to-end smoke check against a running local app:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-test.ps1
```

## Folder structure

```text
SalefnyMVC/
├── Controllers/          # MVC controllers
├── Data/                 # SalefnyDbContext, Identity configuration, seed data
├── Migrations/           # EF Core migrations and model snapshot
├── Models/               # Entities, enums, options, view models
├── Services/             # BusinessRules, EscrowService, NotificationService, ...
├── Views/                # Razor views and shared layouts/partials
├── wwwroot/              # Static assets and uploaded images
├── Properties/           # launchSettings.json (http profile: localhost:5013)
├── Tests/                # xUnit + Moq unit tests
├── docs/                 # ARCHITECTURE.md and this file
├── scripts/              # smoke-test.ps1
├── appsettings.json
├── Program.cs
├── SalefnyMVC.csproj
└── SalefnyMVC.slnx
```
