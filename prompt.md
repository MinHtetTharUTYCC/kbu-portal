# KBU Student Portal — Project Documentation

## Overview

A fully functional university student portal built with **ASP.NET Core MVC** as a learning project covering auth, roles, EF Core, SQL Server, and Razor Views.

**Goal:** Learn ASP.NET Core MVC patterns through a real-world project relevant to daily university life at KBU (Kasem Bundit University).

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC (.NET 8) |
| Auth | ASP.NET Core Identity |
| ORM | Entity Framework Core 8 |
| Database | SQL Server (LocalDB for dev) |
| UI | Bootstrap 5 + Razor Views |
| Real-time (optional) | SignalR |
| Testing (optional) | xUnit |

---

## Roles

| Role | Description |
|---|---|
| `Admin` | Manages users, announcements, subjects |
| `Teacher` | Enters grades, views their subjects |
| `Student` | Views grades, schedule, announcements |

---

## Features

### Auth
- Register (Student only via public form)
- Login / Logout
- Role-based access control (`[Authorize(Roles = "Admin")]`)
- Admin creates Teacher accounts
- Password hashing via ASP.NET Core Identity (built-in)
- Remember me, forgot password (optional)

### Announcements
- Admin creates/edits/deletes announcements
- Target by role (All / Students / Teachers)
- Students and Teachers view relevant announcements
- Pinned announcements (priority flag)

### Grades
- Admin assigns Students to Subjects
- Teacher enters/updates grades for their subjects only
- Student views own grades only (cannot see others)
- Grade includes: Score (0-100), Letter Grade (A/B/C/D/F), Semester, Year
- GPA calculation per semester

### Schedule
- Admin creates timetable entries
- Student views their own weekly schedule
- Schedule shows: Subject, Teacher, Room, Day, Time

### Profile
- Student/Teacher edits own profile
- Photo upload (stored in wwwroot/uploads)
- Student ID, Faculty, Major fields

### Admin Dashboard
- User management (list, create, edit, delete, assign roles)
- Subject management (CRUD)
- Overview stats (total students, teachers, subjects)

---

## Database Schema

```
AspNetUsers (Identity — built-in)
├── Id (string, GUID)
├── UserName
├── Email
├── PasswordHash
└── + custom fields below

ApplicationUser : IdentityUser
├── FirstName
├── LastName
├── StudentId (nullable, students only)
├── Faculty
├── Major
└── ProfilePhoto

Subject
├── Id (int, PK)
├── Name (string)
├── Code (string, unique) e.g. "CS101"
├── Credits (int)
└── TeacherId (FK → ApplicationUser)

StudentSubject (junction table)
├── StudentId (FK → ApplicationUser)
└── SubjectId (FK → Subject)

Grade
├── Id (int, PK)
├── StudentId (FK → ApplicationUser)
├── SubjectId (FK → Subject)
├── Score (decimal)
├── LetterGrade (string) — computed
├── Semester (int) — 1 or 2
└── Year (int)

Announcement
├── Id (int, PK)
├── Title (string)
├── Content (string)
├── CreatedAt (DateTime)
├── CreatedById (FK → ApplicationUser)
├── IsPinned (bool)
└── TargetRole (string) — "All", "Student", "Teacher"

Schedule
├── Id (int, PK)
├── SubjectId (FK → Subject)
├── DayOfWeek (enum) — Mon/Tue/Wed/Thu/Fri
├── StartTime (TimeSpan)
├── EndTime (TimeSpan)
└── Room (string)
```

---

## Project Structure

```
KBUPortal/
├── Controllers/
│   ├── HomeController.cs           — landing, dashboard
│   ├── AccountController.cs        — login, register, logout
│   ├── AnnouncementsController.cs  — CRUD announcements
│   ├── GradesController.cs         — grade entry + viewing
│   ├── ScheduleController.cs       — timetable
│   ├── ProfileController.cs        — user profile
│   └── AdminController.cs          — user + subject management
│
├── Models/
│   ├── ApplicationUser.cs          — extends IdentityUser
│   ├── Subject.cs
│   ├── Grade.cs
│   ├── Announcement.cs
│   ├── Schedule.cs
│   └── StudentSubject.cs
│
├── ViewModels/                      — DTOs for Views (never expose models directly)
│   ├── Account/
│   │   ├── LoginViewModel.cs
│   │   └── RegisterViewModel.cs
│   ├── Grades/
│   │   ├── GradeEntryViewModel.cs
│   │   └── GradeReportViewModel.cs
│   ├── Schedule/
│   │   └── WeeklyScheduleViewModel.cs
│   ├── Announcements/
│   │   └── AnnouncementViewModel.cs
│   └── Admin/
│       └── UserManagementViewModel.cs
│
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml           — root layout (like Next.js layout.tsx)
│   │   ├── _NavBar.cshtml           — navigation partial
│   │   └── _Notifications.cshtml   — notification partial
│   ├── Home/
│   │   └── Index.cshtml             — dashboard
│   ├── Account/
│   │   ├── Login.cshtml
│   │   └── Register.cshtml
│   ├── Announcements/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml
│   │   └── Edit.cshtml
│   ├── Grades/
│   │   ├── Index.cshtml             — student views own grades
│   │   └── Enter.cshtml             — teacher enters grades
│   ├── Schedule/
│   │   └── Index.cshtml             — weekly timetable view
│   ├── Profile/
│   │   └── Index.cshtml
│   └── Admin/
│       ├── Users.cshtml
│       └── Subjects.cshtml
│
├── Data/
│   ├── AppDbContext.cs              — DbContext
│   └── SeedData.cs                 — seed admin user + sample data
│
├── Services/
│   ├── IGradeService.cs
│   ├── GradeService.cs
│   ├── IAnnouncementService.cs
│   ├── AnnouncementService.cs
│   ├── IScheduleService.cs
│   └── ScheduleService.cs
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── uploads/                    — profile photos
│
├── appsettings.json                 — connection string, config
└── Program.cs                       — DI setup, middleware pipeline
```

---

## Build Order

### Phase 1 — Foundation
```
1. Create ASP.NET Core MVC project
2. Install packages (EF Core, Identity, SQL Server)
3. Set up AppDbContext
4. Configure Identity in Program.cs
5. Run initial migration
6. Seed admin user
```

### Phase 2 — Auth
```
7. Register page (Student)
8. Login / Logout
9. Role setup (Admin, Teacher, Student)
10. [Authorize] attribute on controllers
11. Role-based redirects after login
```

### Phase 3 — Core Features
```
12. Announcements CRUD (Admin) + list view (All)
13. Subject management (Admin)
14. Assign students to subjects (Admin)
15. Grade entry (Teacher — own subjects only)
16. Grade view (Student — own grades only)
17. Weekly schedule view (Student)
```

### Phase 4 — Polish
```
18. Profile page + photo upload
19. Admin dashboard with stats
20. GPA calculation
21. Bootstrap UI cleanup
22. SeedData with sample students/grades/schedule
```

### Phase 5 — Optional
```
23. SignalR — real-time notification when new announcement posted
24. Export grades to PDF/Excel
25. Email confirmation on register
```

---

## Key ASP.NET Core Concepts Covered

| Concept | Where Used |
|---|---|
| `[Authorize(Roles = "Admin")]` | AdminController, GradesController |
| `ViewModels` | Every view — never expose raw models |
| `Tag Helpers` | Forms, links, validation messages |
| `Partial Views` | `_NavBar`, `_Notifications` |
| `_Layout.cshtml` | Root layout shared across all pages |
| `TempData` | Flash messages after redirect |
| `ModelState.IsValid` | Form validation |
| `IFormFile` | Profile photo upload |
| `UserManager<T>` | Identity user operations |
| `SignInManager<T>` | Login/logout |
| `RoleManager<T>` | Role assignment |
| EF Core `Include()` | Eager loading relations |
| EF Core Migrations | Schema management |

---

## NestJS → ASP.NET Core MVC Mapping

| NestJS | ASP.NET Core MVC |
|---|---|
| `@Controller()` | `Controller` base class |
| `@Get()`, `@Post()` | `[HttpGet]`, `[HttpPost]` or just action name |
| `@Injectable()` | Registered in `Program.cs` DI |
| `*.service.ts` | `*Service.cs` |
| `*.dto.ts` | `*ViewModel.cs` |
| `PrismaService` | `AppDbContext` |
| `passport-jwt` | `ASP.NET Core Identity` |
| `@Roles()` guard | `[Authorize(Roles = "")]` |
| `layout.tsx` | `_Layout.cshtml` |
| `app.module.ts` | `Program.cs` |
| `.env` | `appsettings.json` |

---

## Packages to Install

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design
```

---

## Connection String (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KBUPortal;Trusted_Connection=True;"
  }
}
```

---

## Notes

- Use **LocalDB** for development (no SQL Server install needed)
- **Never expose `ApplicationUser` directly to views** — always use ViewModels
- **SeedData** should create: 1 Admin, 2 Teachers, 5 Students, 3 Subjects, sample grades and schedule
- Grade letter calculation: A=90+, B=80+, C=70+, D=60+, F=below 60
- All DB calls should be `async/await`
- Services registered as `AddScoped<>` in Program.cs