# AcxiomCRM - Enterprise Customer Relationship Management System

AcxiomCRM is a full-featured, enterprise-grade Customer Relationship Management (CRM) application built strictly in accordance with `AcxiomCRM_Complete_Project_Assignment.pdf`.

It covers the complete sales lifecycle: **Lead Capture & Qualification → Lead-to-Customer Conversion → Customer Management → Opportunity Pipeline with Weighted Calculations → Follow-Ups & Activity Logging → Interactive Analytics Dashboard with Chart.js → 8 Filterable Reports with CSV Export → Role-Based Security Scoping → Append-Only Audit Logging → OpenAPI Swagger REST APIs**.

---

## 🛠️ Technology Stack

- **Framework**: C# ASP.NET Core 8.0 MVC (`net8.0`)
- **Authentication & Security**: ASP.NET Core Identity with PBKDF2 password hashing, account lockout (5 failed attempts, 15-minute lock), anti-forgery protection, and Dual-Scheme Authentication (Cookie for MVC and JWT Bearer for REST APIs)
- **ORM & Database**: Entity Framework Core 8.0 with SQL Server / LocalDB (`AcxiomCRM_DB`)
- **User Interface**: Bootstrap 5, Bootstrap Icons, responsive sidebar layout, card-based metrics, and quick-login credential pre-fill buttons
- **Visualization**: Chart.js 4.x for interactive lead status doughnuts, pipeline stage bars, and monthly revenue trends
- **REST APIs & Documentation**: Swagger / OpenAPI (Swashbuckle) with interactive JWT Bearer authorization
- **Validation**: Unobtrusive Client-Side Validation (`custom-validators.js`) + Server-Side Validation (`DataAnnotations` and Business Logic Services)

---

## 👥 Default Test Accounts & Role Hierarchy

The application automatically checks, applies EF Core database migrations, and seeds the required roles and demo records on startup:

| Role | Username / Email | Password | Access Scope & Permissions |
|---|---|---|---|
| **Admin** | `admin@acxiom.com` | `Admin@123` | Full access across all modules: User & Role management, Audit Logs, all customers, leads, opportunities, follow-ups, activities, and all 8 reports. |
| **Manager** | `manager@acxiom.com` | `Manager@123` | Supervisory access: Views team-wide customers, leads, opportunities, follow-ups, activities, audit logs, and executive reports. Cannot manage users. |
| **SalesExecutive** | `sales@acxiom.com` | `Sales@123` | Scoped sales access: Only views and manages customers, leads, opportunities, follow-ups, and activities assigned to them. Blocked from `/Users` and `/AuditLogs`. |

> **Quick Login Tip:** On the Login page (`/Account/Login`), one-click demo buttons are provided to instantly populate the form for Admin, Manager, or Sales Executive credentials.

---

## 📋 Comprehensive Module Breakdown

### 1. Authentication & Security
- Secure login and logout with ASP.NET Core Identity cookie sessions.
- Strict password policy: Minimum 8 characters, uppercase, lowercase, digit, and special character.
- Lockout mechanism: Accounts automatically locked for 15 minutes after 5 consecutive failed attempts.
- Role-based authorization via `[Authorize(Roles = "...")]` and contextual UI visibility.
- Audit trail logging for every login, failed attempt, lockout, and logout event.

### 2. Dashboard & Analytics
- **8 KPI Summary Cards**: Total Customers, Total Leads, Converted Leads, Open Opportunities, Total Pipeline Value, Weighted Pipeline Value, Overdue Follow-ups, and Total Activities.
- **3 Dynamic Chart.js Visualizations**:
  - *Lead Status Breakdown* (Doughnut Chart)
  - *Pipeline by Stage* (Bar Chart)
  - *Monthly Won Revenue Trend* (Line Chart)
- **Interactive Filtering**: Filter dashboard metrics and charts by Date Range (Last 7 Days, Last 30 Days, This Quarter, This Year, or Custom Range).
- **Recent Activities & Urgent Tasks Feed**: Displays today's upcoming follow-ups and latest activities.

### 3. Customer Management
- Complete CRUD operations (Create, List, Details, Edit, Deactivate).
- System-generated unique codes (`CUST-1001`, etc.).
- Strict validations: Mandatory customer name, unique email, 10-digit Indian phone format (`^[6-9]\d{9}$`), and duplicate customer checks.
- Comprehensive customer 360 view with interaction timeline (linked leads, opportunities, follow-ups, and logged activities).

### 4. Lead Management & Conversion Workflow
- Lead capture with source tracking (Website, Referral, Cold Call, Campaign, Partner), qualification stages (New, Contacted, Qualified, Proposal, Won, Lost), and priority flags.
- **Lead-to-Customer Conversion**:
  - One-click workflow converts qualified leads into full Customer records.
  - Automatically creates a linked Opportunity with custom deal amount, probability, and expected close date.
  - Updates lead status to `Converted` and records `ConvertedDate`, `ConvertedCustomerId`, and `ConvertedOpportunityId`.
  - **Business Invariant**: Once converted, a lead cannot be reverted back to an earlier pipeline status.

### 5. Opportunity Management & Pipeline Tracking
- Deal tracking with 6 pipeline stages: *Prospecting*, *Qualification*, *Needs Analysis*, *Proposal*, *Negotiation*, *Closed Won*, *Closed Lost*.
- Business Rules:
  - Deal Amount must be greater than 0 (`Amount > 0`).
  - Probability must be between 0% and 100%.
  - Expected Close Date cannot be set in the past for active opportunities.
- **Weighted Revenue Calculation**: Automatically calculates and tracks `WeightedAmount = Amount * (Probability / 100)` across individual records, pipeline summaries, and analytics charts.

### 6. Follow-Up Management
- Multi-channel follow-ups: *Call*, *Meeting*, *Email*, *Task*.
- Business Rule: Follow-up scheduled date cannot be set earlier than the current date for newly scheduled follow-ups.
- Tracking views: *Today's Follow-ups*, *Upcoming Follow-ups*, and *Overdue Follow-ups*.
- In-place quick actions: Mark as Completed (with completion notes) or Reschedule to a future date.

### 7. Activity Management
- Log historical interactions with customers and leads: *Calls*, *Meetings*, *Emails*, and *Tasks*.
- Tracks outcome notes, activity duration, assigned representative, and timestamp.

### 8. User & Role Administration (Admin Only)
- User list with role indicators, lockout status badges, and last active tracking.
- Create new users and assign roles (`Admin`, `Manager`, `SalesExecutive`).
- Edit user profile details and role assignments.
- Administrative password reset and account unlock for locked-out users.

### 9. Append-Only Audit Logging
- Dedicated `AuditLog` entity capturing `AuditLogId`, `UserId`, `UserName`, `Action`, `EntityName`, `RecordId`, `OldValue`, `NewValue`, `IpAddress`, `Result`, and `CreatedDate`.
- **Append-Only Architecture**: The audit log store has no update or delete operations exposed.
- Filterable audit viewer for Admin and Manager by User, Entity/Module, Action, and Date Range.

### 10. Reports & Data Export (8 Modules)
Full-featured reporting hub (`/Reports`) with interactive filtering, print layout, and **instant CSV export**:
1. **Customer Report**: Status, company, assigned owner, and creation date.
2. **Lead Report**: Source, status, priority, expected deal value, owner, and conversion status.
3. **Follow-Up Report**: Type, customer, scheduled date, overdue flag, status, and remarks.
4. **Opportunity Report**: Stage, deal amount, probability percentage, weighted value, and expected close date.
5. **Pipeline Report**: Stage-wise and owner-wise deal counts, total pipeline amount, and weighted revenue.
6. **Sales / Conversion Report**: Total leads vs converted, lead conversion rate, total opportunities vs won, opportunity win rate, and total won revenue.
7. **User Activity Report**: Activity breakdown per team member (Calls, Meetings, Emails, Tasks, and total actions).
8. **Audit Report**: Security, authentication, and entity change logs with actor details and results.

### 11. REST APIs & Swagger Documentation
Enterprise REST endpoints secured via JWT Bearer tokens and cookie authentication:
- `POST /api/auth/login`: Authenticates user credentials and returns JWT Bearer token + user roles.
- `POST /api/auth/logout`: Revokes session.
- `GET /api/customers`, `GET /api/customers/{id}`, `POST /api/customers`, `PUT /api/customers/{id}`, `DELETE /api/customers/{id}`
- `GET /api/leads`, `GET /api/leads/{id}`, `POST /api/leads`
- `GET /api/opportunities`, `GET /api/opportunities/{id}`, `POST /api/opportunities`
- `GET /api/followups`, `GET /api/followups/{id}`, `POST /api/followups`
- `GET /api/reports/pipeline`: Returns live total pipeline amount, weighted revenue, and stage-wise summaries.
- Interactive Swagger UI available at `/swagger`.

---

## 🚀 Setup & Execution Guide

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB `(localdb)\mssqllocaldb` or full instance `localhost`)

### 1. Database Configuration
The database connection string is located in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AcxiomCRM_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 2. Running Database Migrations
Database migrations are automatically verified and applied on startup. To manually update the database:
```bash
dotnet ef database update
```

### 3. Starting the Application
```bash
dotnet run
```
The application will launch and output the listening URL (e.g., `http://localhost:5207` or `http://127.0.0.1:5124`).

### 4. Accessing the System
- **Web Application**: `http://localhost:5207` or `http://127.0.0.1:5124`
- **Swagger API Documentation**: `http://localhost:5207/swagger`

---

## 🧪 Automated Test Verification Suites

The repository includes complete automated test scripts verifying every tier of the application:

### Test Suite 1: REST API & Business Validation Suite (`test_crm.ps1`)
Verifies Swagger schema, JWT authentication, Customer/Lead/Opportunity/Follow-up CRUD, 400 Bad Request on zero/negative amounts, 400 Bad Request on probability > 100, 409 Conflict on duplicate customer emails/phones, pipeline report calculations, and Sales Executive data scoping:
```powershell
powershell -ExecutionPolicy Bypass -File .\test_crm.ps1
```
*(Result: 14/14 tests passing).*

### Test Suite 2: MVC Cookie Authentication & Role Security (`test_mvc_roles.ps1`)
Verifies Anti-Forgery tokens, Cookie session generation, Admin access across all 9 controllers, Sales Executive access restriction on `/Users` and `/AuditLogs` (redirect to AccessDenied), and Manager authorization:
```powershell
powershell -ExecutionPolicy Bypass -File .\test_mvc_roles.ps1
```

### Test Suite 3: All 8 CSV Report Exports (`test_debug_report.ps1`)
Verifies that all 8 report endpoints return HTTP 200 with `text/csv` headers and correct tabular CSV data:
```powershell
powershell -ExecutionPolicy Bypass -File .\test_debug_report.ps1
```

---

## 📂 Project Directory Structure

```
AcxiomCRM/
│
├── Controllers/
│   ├── AccountController.cs          # Authentication, Login, Register, Lockout, AccessDenied
│   ├── CustomersController.cs        # Customer CRUD, Scoping, Validation, History
│   ├── LeadsController.cs            # Lead Management & Lead-to-Customer Conversion
│   ├── OpportunitiesController.cs    # Deals, Stages, Weighted Pipeline Calculation
│   ├── FollowUpsController.cs        # Follow-ups, Due Dates, Complete & Reschedule Actions
│   ├── ActivitiesController.cs       # Calls, Meetings, Emails, Tasks Logging
│   ├── DashboardController.cs        # KPI Cards, Date Filters, Chart.js Visualizations
│   ├── UsersController.cs            # Admin User Management, Role Assignments, Unlocks
│   ├── AuditLogsController.cs        # Append-Only Audit Trail Viewer & Filters
│   ├── ReportsController.cs          # 8 Reporting Modules with CSV Download
│   └── Api/                          # REST API Controllers (Auth, Customers, Leads, Opps, FollowUps, Reports)
│
├── Data/
│   ├── ApplicationDbContext.cs      # EF Core DbContext with Relations & Indexes
│   ├── DbInitializer.cs              # Role & Default User Seeding, Sample Records
│   └── Migrations/                   # EF Core Code-First Migrations
│
├── Models/                           # ApplicationUser, ApplicationRole, Customer, Lead, Opportunity, FollowUp, Activity, AuditLog
├── ViewModels/                       # Strongly-Typed ViewModels for Forms & Hubs
├── DTOs/                             # Request/Response DTOs for REST APIs
├── Services/                         # IAuditService, IUserScopeService, IExportService
├── Views/                            # Bootstrap 5 Razor Views with Chart.js Integration
├── wwwroot/                          # CSS, JavaScript (Chart.js, custom-validators.js), Assets
├── test_crm.ps1                      # Automated REST API Verification Suite
├── test_mvc_roles.ps1                # Automated MVC Role-Based Security Suite
├── test_debug_report.ps1             # Automated CSV Export Verification Suite
└── REQUIREMENT_COVERAGE.md           # Complete Requirement Traceability Matrix
```

---

## ✅ Assignment Requirement Compliance

All functional modules, technical constraints, business validation rules, security requirements, and acceptance test cases from `AcxiomCRM_Complete_Project_Assignment.pdf` have been fully developed, integrated, verified, and documented. For complete traceability from PDF pages to source code, refer to [REQUIREMENT_COVERAGE.md](file:///c:/Users/NIDHI%20KEDIA/OneDrive/Desktop/Acxiom/REQUIREMENT_COVERAGE.md).
