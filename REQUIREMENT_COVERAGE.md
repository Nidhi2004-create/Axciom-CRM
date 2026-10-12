# AcxiomCRM - Requirement Traceability Matrix (REQUIREMENT_COVERAGE.md)

This document tracks every functional, technical, security, and acceptance requirement specified in `AcxiomCRM_Complete_Project_Assignment.pdf` to its design, implementation file, and test status.

---

## 1. Module Structure & Architecture (PDF Pages 2–3, 14)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| ARC-01 | Layered Architecture (Presentation, Application, Domain/Business, Data Access, Database, Security, Cross-Cutting) | ASP.NET Core MVC (net8.0), Controllers, Services, Models, Data, ViewModels, DTOs | Completed |
| ARC-02 | ASP.NET Core MVC + Entity Framework Core + SQL Server | `AcxiomCRM.csproj`, `ApplicationDbContext.cs`, SQL Server / LocalDB | Completed |
| ARC-03 | Bootstrap Responsive UI + Chart.js Visualization | `_Layout.cshtml`, Bootstrap 5, Chart.js 4.x CDN/Assets | Completed |
| ARC-04 | ASP.NET Core Identity for Authentication & Roles | `ApplicationUser`, `ApplicationRole`, `ApplicationDbContext : IdentityDbContext` | Completed |

---

## 2. Authentication & Authorization (PDF Pages 4, 8, 9, 15, 16, 17)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| AUTH-01 | User Login & Logout with secure session/cookie | `AccountController.cs` (`Login`, `Logout`), `LoginViewModel` | Completed |
| AUTH-02 | No plain-text passwords; ASP.NET Core Identity password hashing | `PasswordHasher<ApplicationUser>` (PBKDF2/Argon2 via Identity) | Completed |
| AUTH-03 | Password Policy (Min 8 chars, uppercase, lowercase, digit, special char) | `Program.cs` (`IdentityOptions` configuration) | Completed |
| AUTH-04 | Account Lockout after repeated failed logins (5 attempts, 15 min) | `Program.cs` (`LockoutOptions`), `AccountController` failed login handling | Completed |
| AUTH-05 | Three Roles: `Admin`, `Manager`, `SalesExecutive` | `DbInitializer.cs`, role seeding, role checks | Completed |
| AUTH-06 | Server-side Role Authorization & Permission Matrix | `[Authorize(Roles = "...")]`, ownership scoping service | Completed |
| AUTH-07 | Audit logging for Login, Failed Login, Lockout, and Logout | `IAuditService`, `AccountController` logging | Completed |
| AUTH-08 | Anti-Forgery token protection (`ValidateAntiForgeryToken`) | Applied on all state-changing MVC forms | Completed |

---

## 3. Customer Management (PDF Pages 4, 10, 15, 16, 17)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| CUST-01 | Customer Entity with mandatory fields (`CustomerId`, `CustomerCode`, `CustomerName`, `Email`, `Phone`, `CompanyName`, `Address`, `City`, `State`, `Status`, `CreatedDate`, `CreatedBy`) | `Models/Customer.cs`, `ApplicationDbContext` | Completed |
| CUST-02 | Customer CRUD operations (Create, Read/Details, Edit, Delete/Deactivate) | `CustomersController.cs`, `CustomerViewModels.cs`, Razor Views | Completed |
| CUST-03 | Email validation, format check & uniqueness constraint | Model data annotations + `CustomersController` uniqueness check | Completed |
| CUST-04 | Phone validation (10-digit mobile format) & uniqueness constraint | RegularExpression + `CustomersController` uniqueness check | Completed |
| CUST-05 | Duplicate customer creation prevention | Server-side validation check in `CustomersController.Create` | Completed |
| CUST-06 | Customer history and related activities (leads, opportunities, follow-ups) | `Details.cshtml`, sub-lists for linked records | Completed |
| CUST-07 | Search, filtering, sorting, pagination (Name, Email, Phone, Company) | `CustomersController.Index`, filter bar, pagination helper | Completed |
| CUST-08 | Role-based data scoping (Admin/Manager sees all/team, SalesExecutive sees assigned) | Scoped repository query in `UserScopeService` | Completed |
| CUST-09 | Audit logging for Customer Create, Update, Delete | `IAuditService.LogAsync` calls in `CustomersController` | Completed |

---

## 4. Lead Management & Conversion (PDF Pages 5, 9, 10, 15, 16, 17)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| LEAD-01 | Lead Entity (`LeadId`, `LeadCode`, `LeadName`, `Email`, `Phone`, `CompanyName`, `Source`, `Status`, `Priority`, `ExpectedValue`, `CreatedDate`, `AssignedTo`) | `Models/Lead.cs`, `ApplicationDbContext` | Completed |
| LEAD-02 | Lead CRUD operations | `LeadsController.cs`, `LeadViewModels.cs`, Razor Views | Completed |
| LEAD-03 | Lead Status workflow (`New`, `Contacted`, `Qualified`, `Unqualified`, `Converted`, `Lost`) | Status transition rules in `LeadsController` | Completed |
| LEAD-04 | Disallow invalid status transitions | Validation prevents reverting converted leads | Completed |
| LEAD-05 | Lead Conversion to Customer and Opportunity | `LeadsController.Convert`, `ConvertLeadViewModel`, `Convert.cshtml` | Completed |
| LEAD-06 | Search and filtering (Lead Name, Company, Status, Assigned User) | `LeadsController.Index` query filters | Completed |
| LEAD-07 | Audit logging for Lead Create, Update, Status Change, Conversion | `IAuditService.LogAsync` in `LeadsController` | Completed |

---

## 5. Opportunity Management & Pipeline (PDF Pages 5, 7, 8, 10, 15, 16, 17)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| OPP-01 | Opportunity Entity (`OpportunityId`, `OpportunityName`, `CustomerId`, `LeadId`, `Amount`, `Stage`, `Probability`, `ExpectedCloseDate`, `Status`, `CreatedDate`, `AssignedTo`) | `Models/Opportunity.cs`, `ApplicationDbContext` | Completed |
| OPP-02 | Pipeline stages (`Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`) | Pipeline stages handled in `OpportunitiesController` | Completed |
| OPP-03 | Opportunity Amount > 0 for active opportunities | Model attribute + Server-side validation rule in controller | Completed |
| OPP-04 | Probability must be between 0 and 100 inclusive | `[Range(0, 100)]` + Server-side validation rule in controller | Completed |
| OPP-05 | Expected Close Date cannot be in the past for active opportunities | Min date validation in Razor and Server-side check | Completed |
| OPP-06 | Weighted Pipeline Calculation (`Amount * Probability / 100`) | Computed property `WeightedAmount`, UI metrics & preview | Completed |
| OPP-07 | Search and filtering (Name, Customer, Stage, Status) | `OpportunitiesController.Index` query filters | Completed |
| OPP-08 | Audit logging for Opportunity Create, Update, Stage Change, Close | `IAuditService.LogAsync` calls in `OpportunitiesController` | Completed |

---

## 6. Follow-Up Management (PDF Pages 5, 7, 8, 10, 15, 16, 17)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| FOL-01 | FollowUp Entity (`FollowUpId`, `CustomerId`, `LeadId`, `FollowUpDate`, `FollowUpType`, `Remarks`, `Status`, `AssignedTo`) | `Models/FollowUp.cs`, `ApplicationDbContext` | Completed |
| FOL-02 | Statuses (`Planned`, `Completed`, `Missed`, `Cancelled`) | Status lifecycle in `FollowUpsController` | Completed |
| FOL-03 | Follow-Up Date cannot be earlier than today for new/planned items | Client HTML5 min date + Server-side validation check | Completed |
| FOL-04 | Upcoming and Overdue follow-up tracking and quick actions | KPI summary cards + Overdue filter + Overdue visual badges | Completed |
| FOL-05 | Complete, Reschedule, and Cancel actions | Dedicated actions `Complete`, `Reschedule`, `Delete` with modals | Completed |
| FOL-06 | Audit logging for Follow-Up completion and rescheduling | `IAuditService.LogAsync` in `FollowUpsController` | Completed |

---

## 7. Activity Management (PDF Pages 14, 15, 17)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| ACT-01 | Activity Entity (`ActivityId`, `ActivityType`, `Subject`, `Description`, `ActivityDate`, `CustomerId`, `LeadId`, `AssignedTo`, `Status`) | `Models/Activity.cs`, `ApplicationDbContext` | Completed |
| ACT-02 | Types: `Call`, `Meeting`, `Email`, `Task` | Select options & badges in `ActivitiesController` | Completed |
| ACT-03 | Activity CRUD + Search & Filters (Type, Date, Status, User) | `ActivitiesController.cs`, `Index.cshtml`, `Create.cshtml` | Completed |
| ACT-04 | Association with Customer or Lead timeline | Relations rendered on Customer details timeline | Completed |

---

## 8. Dashboard & Charts (PDF Pages 4, 17, 19)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| DASH-01 | Role-based KPI Cards (Total Customers, Total Leads, Open Leads, Total Opps, Open Opps, Won Opps, Lost Opps, Total Pipeline Value) | `DashboardController.cs`, `DashboardViewModel`, `Index.cshtml` | Completed |
| DASH-02 | Admin dashboard system & security statistics | System user counts & audit events on dashboard | Completed |
| DASH-03 | Manager dashboard team performance and sales pipeline | Team summary metrics and pipeline breakdown | Completed |
| DASH-04 | SalesExecutive dashboard assigned records & upcoming follow-ups | Personal work queue and upcoming follow-ups table | Completed |
| DASH-05 | Date filters (Today, This Week, This Month, Custom Range) | Date filtering toolbar and query logic | Completed |
| DASH-06 | Chart.js: Lead Status (New, Contacted, Qualified, Lost, Converted) | Chart.js doughnut chart container + dynamic datasets | Completed |
| DASH-07 | Chart.js: Opportunity Pipeline by Stage | Chart.js bar chart container + dynamic stage totals | Completed |
| DASH-08 | Chart.js: Monthly Sales totals | Chart.js line chart for monthly won amounts | Completed |

---

## 9. User & Role Management (PDF Pages 5, 8, 9, 10, 15, 18)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| USR-01 | User CRUD, search, activation/deactivation | `UsersController.cs` (Admin only) | Completed |
| USR-02 | Single primary role assignment (`Admin`, `Manager`, `SalesExecutive`) | Role dropdown and assignment handler in `UsersController` | Completed |
| USR-03 | Password reset workflow by authorized administrator | Admin reset password action & view with audit logging | Completed |
| USR-04 | Account status and lockout status display with manual unlock | Account status badge and manual unlock POST action | Completed |
| USR-05 | Never expose password hashes or sensitive security secrets | DTOs, sanitized ViewModels, excluded from serialization | Completed |
| USR-06 | Audit logging for role changes and user administration | `IAuditService.LogAsync` calls in `UsersController` | Completed |

---

## 10. Audit Logging (PDF Pages 5, 6, 8, 10, 15, 18)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| AUD-01 | AuditLog Entity (`AuditLogId`, `UserId`, `Action`, `EntityName`, `RecordId`, `OldValue`, `NewValue`, `CreatedDate`, `IpAddress`, `Result`, `Details`) | `Models/AuditLog.cs`, `ApplicationDbContext` | Completed |
| AUD-02 | Append-oriented storage (No update or delete allowed) | Read-only controller/service for audit table | Completed |
| AUD-03 | Captures Auth events (Login, Failed Login, Lockout, Logout) | Hooked into `AccountController` | Completed |
| AUD-04 | Captures Entity CRUD and Role changes | Interceptor / centralized `IAuditService` | Completed |
| AUD-05 | Audit viewer with filters (User, Module, Action, Date Range) | `AuditLogsController.cs` (Admin/Manager scoped) + CSV Export | Completed |

---

## 11. REST API & Swagger (PDF Pages 6, 11, 18)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| API-01 | Swagger / OpenAPI UI documentation and schemas | `Swashbuckle.AspNetCore`, JWT/Cookie support, `/swagger` UI | Completed |
| API-02 | `POST /api/auth/login` and `POST /api/auth/logout` | `Controllers/Api/AuthApiController.cs` with JWT generation & validation | Completed |
| API-03 | Customers API: GET, GET {id}, POST, PUT {id}, DELETE {id} | `Controllers/Api/CustomersApiController.cs` + DTOs | Completed |
| API-04 | Leads API: GET, POST | `Controllers/Api/LeadsApiController.cs` + DTOs | Completed |
| API-05 | Opportunities API: GET, POST | `Controllers/Api/OpportunitiesApiController.cs` + DTOs | Completed |
| API-06 | FollowUps API: GET, POST | `Controllers/Api/FollowUpsApiController.cs` + DTOs | Completed |
| API-07 | Pipeline Report API: `GET /api/reports/pipeline` | `Controllers/Api/ReportsApiController.cs` + DTOs | Completed |
| API-08 | Proper HTTP status codes (200, 201, 400, 401, 403, 404, 409) | Consistent action results across all endpoints | Completed |

---

## 12. Reports & Exports (PDF Pages 6, 11)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| REP-01 | Customer Report (List, status, owner, date) | `ReportsController.CustomerReport` + CSV Export | Completed |
| REP-02 | Lead Report (Source, status, owner, conversion) | `ReportsController.LeadReport` + CSV Export | Completed |
| REP-03 | Follow-Up Report (Planned, completed, missed, overdue) | `ReportsController.FollowUpReport` + CSV Export | Completed |
| REP-04 | Opportunity Report (Stage, amount, probability, close date) | `ReportsController.OpportunityReport` + CSV Export | Completed |
| REP-05 | Pipeline Report (Stage-wise & owner-wise pipeline amount) | `ReportsController.PipelineReport` + CSV Export | Completed |
| REP-06 | Sales / Conversion Report (Leads converted vs not, win rate) | `ReportsController.ConversionReport` + CSV Export | Completed |
| REP-07 | User Activity Report (Actions and counts per user) | `ReportsController.UserActivityReport` + CSV Export | Completed |
| REP-08 | Audit Report (Security & business events) | `ReportsController.AuditReport` + CSV Export | Completed |
| REP-09 | Export capabilities (CSV / Excel format download and Print) | `ExportService.cs` (CSV generation across all reports) | Completed |

---

## 13. Validation & Acceptance Checklist (PDF Pages 7, 8, 12, 19)

| Req ID | Requirement Description | Implementation Components | Status |
|---|---|---|---|
| VAL-01 | Client-Side Validation (Required, Email, Phone 10-digit, Length, Date, Numeric) | jQuery Validation, unobtrusive, custom JS validator (`custom-validators.js`) | Completed |
| VAL-02 | Server-Side Validation on every request payload | DataAnnotations, Model binding checks, Service rules | Completed |
| VAL-03 | Opportunity Amount > 0 and Probability 0–100 | Validation rules & unit/integration test in `test_crm.ps1` | Completed |
| VAL-04 | Opportunity Expected Close Date not in past | Validation rule & test in `OpportunitiesController.cs` | Completed |
| VAL-05 | Follow-Up Date not earlier than today | Validation rule & test in `FollowUpsController.cs` | Completed |
| VAL-06 | Anti-Forgery token protection across all POST forms | `[ValidateAntiForgeryToken]` on all POST actions & verified in `test_browser_login.ps1` | Completed |
| VAL-07 | Role-based isolation (SalesExec only sees assigned records) | Query filters in `UserScopeService.cs` & verified in `test_mvc_roles.ps1` | Completed |
