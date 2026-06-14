# ViewModels Folder — KBU Portal

## What are ViewModels?

ViewModels are **data transfer objects (DTOs)** that shape what a View receives. They sit between your database Models and your Views.

- **Models** = database structure (all columns, FKs, navigation properties) → maps to DB tables
- **ViewModels** = what the view actually needs (subset of fields, computed values, upload fields) → maps to forms/pages

**NestJS equivalent:** Like DTOs you'd define with `class-validator` decorators for request/response shaping.

---

## Why separate Models from ViewModels?

Take `ApplicationUser` as an example. The Model has:
- `PasswordHash` (never show in a view)
- `SecurityStamp`, `ConcurrencyStamp` (internal Identity fields)
- `PhoneNumber`, `EmailConfirmed` (not needed in most views)

But a Profile view needs:
- `FirstName`, `LastName`, `Email` (from Model)
- `Role` (computed — not even a column)
- `PhotoFile` (an `IFormFile` for upload — doesn't exist in the Model at all)

Without ViewModels, you'd either expose sensitive fields or lose type safety. ViewModels solve this by giving you a **clean, purpose-built class per view**.

---

## The 8 ViewModel Groups

### 1. `ViewModels/Account/` — Authentication

**`LoginViewModel.cs`** (16 lines)
```csharp
public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
```
- 3 fields — that's it. No `FirstName`, no `StudentId`, just what the login form needs.
- `[EmailAddress]` — generates `<input type="email">` via Tag Helper
- `[DataType(DataType.Password)]` — generates `<input type="password">`
- Used by: `AccountController.Login()` (line 43)
- Maps to: `Views/Account/Login.cshtml`

**`RegisterViewModel.cs`** (37 lines)
```csharp
public class RegisterViewModel
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(50)]
    public string? StudentId { get; set; }

    [StringLength(100)]
    public string? Faculty { get; set; }

    [StringLength(100)]
    public string? Major { get; set; }
}
```
- `[Compare(nameof(Password))]` — auto-validates that `ConfirmPassword` matches `Password`
- `StudentId`, `Faculty`, `Major` are nullable (`string?`) — optional during registration
- Used by: `AccountController.Register()` (line 19)
- Maps to: `Views/Account/Register.cshtml`

---

### 2. `ViewModels/Admin/` — Dashboard

**`AdminDashboardViewModel.cs`** (42 lines) — contains 3 classes in one file:

```csharp
public class AdminDashboardViewModel
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalSubjects { get; set; }
    public int TotalAnnouncements { get; set; }
    public List<RecentAnnouncement> RecentAnnouncements { get; set; } = new();
    public List<SubjectSummary> TopSubjects { get; set; } = new();
}

public class RecentAnnouncement   // nested DTO
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
}

public class SubjectSummary       // nested DTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int EnrolledStudents { get; set; }
    public string TeacherName { get; set; } = string.Empty;
}
```
- **Why nested DTOs?** The dashboard doesn't return full `Announcement` or `Subject` entities — just the fields the view needs. `RecentAnnouncement` has `CreatedBy` (a string name), not `CreatedById` (a FK).
- The controller builds these via LINQ `.Select()` projection at `AdminController.Dashboard()` — the DB only returns the needed columns.
- Used by: `AdminController.Dashboard()`
- Maps to: `Views/Admin/Dashboard.cshtml`

---

### 3. `ViewModels/Subjects/` — Course CRUD

**`SubjectViewModel.cs`** (24 lines)
```csharp
public class SubjectViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Range(1, 12)]
    public int Credits { get; set; } = 3;

    [StringLength(450)]
    public string? TeacherId { get; set; }

    public string? TeacherName { get; set; }
}
```
- `[Range(1, 12)]` — generates `min="1" max="12"` on the `<input>` Tag Helper
- `TeacherId` is `string?` (nullable) — a subject can be unassigned
- `TeacherName` is **display-only** — the controller populates it from the related `ApplicationUser` but the form never submits it
- Default value `Credits = 3` — pre-fills the form
- Used by: all `SubjectsController` actions (lines 25, 48, 56, 101, 130)
- Maps to: `Views/Subjects/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`

---

### 4. `ViewModels/StudentSubjects/` — Enrollment

**`StudentSubjectAssignmentViewModel.cs`** — contains 2 classes:

```csharp
public class StudentSubjectAssignmentViewModel
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public List<StudentAssignmentItem> Students { get; set; } = new();
}

public class StudentAssignmentItem
{
    public string StudentId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAssigned { get; set; }  // checkbox state
}
```
- `IsAssigned` is the key — the view renders checkboxes, the POST handler diffs current vs selected enrollments
- Used by: `StudentSubjectsController.AssignStudents()` (GET and POST)
- Maps to: `Views/StudentSubjects/AssignStudents.cshtml`

---

### 5. `ViewModels/Schedules/` — Weekly Schedule

**`WeeklyScheduleViewModel.cs`** — contains 2 classes:

```csharp
public class WeeklyScheduleViewModel
{
    public List<ScheduleItem> Items { get; set; } = new();
}

public class ScheduleItem
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Room { get; set; } = string.Empty;
}
```
- `DayOfWeek` is a .NET enum (Monday=1, Tuesday=2, etc.)
- `TimeSpan` represents time-of-day (not duration)
- Used by: `ScheduleController.Index()`
- Maps to: `Views/Schedule/Index.cshtml`

---

### 6. `ViewModels/Grades/` — Grade Entry + Report

**`GradeViewModels.cs`** (68 lines) — contains 5 classes:

```csharp
// Teacher side — what the grade entry form sends
public class GradeEntryViewModel
{
    public int SubjectId { get; set; }
    [Required] public string SubjectCode { get; set; } = string.Empty;
    [Required] public string SubjectName { get; set; } = string.Empty;
    public List<StudentGradeItem> StudentGrades { get; set; } = new();
}

public class StudentGradeItem
{
    public string StudentId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? StudentNumber { get; set; }
    public decimal? Score { get; set; }        // nullable — not yet graded
    public string LetterGrade { get; set; } = string.Empty;
    public int Semester { get; set; } = 1;
    public int Year { get; set; }
}

// Student side — what the grade report shows
public class GradeReportViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public List<SemesterGrades> Semesters { get; set; } = new();
}

public class SemesterGrades
{
    public int Semester { get; set; }
    public int Year { get; set; }
    public List<GradeDetail> Grades { get; set; } = new();
    public decimal GPA { get; set; }           // computed by controller
    public decimal TotalCredits { get; set; }  // computed by controller
}

public class GradeDetail
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public decimal Score { get; set; }
    public string LetterGrade { get; set; } = string.Empty;
}
```
- `Score` is `decimal?` (nullable) — a student might not have a grade yet
- `GPA` and `TotalCredits` are **computed by the controller**, not the DB — this is a ViewModel responsibility
- Used by: `GradesController.Enter()` (GET line 50, POST line 85) and `GradesController.MyGrades()` (line 120)
- Maps to: `Views/Grades/Enter.cshtml` and `Views/Grades/MyGrades.cshtml`

---

### 7. `ViewModels/Announcements/` — Announcements

**`AnnouncementViewModel.cs`**
```csharp
public class AnnouncementViewModel
{
    public int Id { get; set; }
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public string TargetRole { get; set; } = "All";
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string CreatedByEmail { get; set; } = string.Empty;
}
```
- `TargetRole` defaults to `"All"` — the form dropdown offers "All", "Student", "Teacher"
- `CreatedByName` and `CreatedByEmail` are display-only — populated by the controller from the related `ApplicationUser`
- Used by: all `AnnouncementsController` actions
- Maps to: `Views/Announcements/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`

---

### 8. `ViewModels/Profile/` — User Profile

**`ProfileViewModel.cs`** (36 lines)
```csharp
public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;
    [Required] [StringLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required] [StringLength(50)] public string LastName { get; set; } = string.Empty;
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;
    [StringLength(50)] public string? StudentId { get; set; }
    [StringLength(100)] public string? Faculty { get; set; }
    [StringLength(100)] public string? Major { get; set; }
    public string? ProfilePhoto { get; set; }    // filename of uploaded photo

    [DataType(DataType.Upload)]
    public IFormFile? PhotoFile { get; set; }    // ← THIS doesn't exist in Model

    public string Role { get; set; } = string.Empty;
}
```
- **`PhotoFile` is the key difference** — it's an `IFormFile` (file upload), which has no equivalent in the database Model. The Model stores the filename (`ProfilePhoto`), but the View needs the actual file input.
- `Role` is computed — the controller looks up the user's role and passes it here
- Used by: `ProfileController.Index()` (line 23) and `ProfileController.Update()` (line 35)
- Maps to: `Views/Profile/Index.cshtml`

---

## Validation Attributes — Complete Reference

| Attribute | What it does | Generates in HTML |
|---|---|---|
| `[Required]` | Field must have a value | `data-val-required="The ... field is required."` |
| `[EmailAddress]` | Must be valid email format | `type="email"` |
| `[StringLength(50)]` | Max 50 characters | `data-val-length-max="50"` |
| `[StringLength(100, MinimumLength = 6)]` | 6-100 characters | `data-val-length-min="6" data-val-length-max="100"` |
| `[Range(1, 12)]` | Number between 1 and 12 | `min="1" max="12"` |
| `[Compare(nameof(Password))]` | Must match another field | `data-val-equalto` |
| `[DataType(DataType.Password)]` | Password field | `type="password"` |
| `[DataType(DataType.Upload)]` | File upload | `type="file"` |

These attributes serve dual purpose:
1. **Server-side validation** — `ModelState.IsValid` checks them in the controller
2. **Client-side validation** — Tag Helpers generate `data-val-*` attributes that jQuery Validation reads

---

## How ViewModel ↔ Controller ↔ View connect

```
Controller:
  var model = new SubjectViewModel { Code = "CS101", Name = "..." };
  return View(model);              // passes ViewModel to View

View:
  @model SubjectViewModel          // declares expected type
  <input asp-for="Code" />         // Tag Helper reads from ViewModel

POST back:
  Controller:
    public IActionResult Create(SubjectViewModel model)  // auto-bound from form
    {
        if (!ModelState.IsValid)   // checks [Required], [StringLength], etc.
            return View(model);    // re-show form with errors
        // save to DB...
    }
```

---

## Folder Structure Convention

```
ViewModels/
  Account/
    LoginViewModel.cs
    RegisterViewModel.cs
  Admin/
    AdminDashboardViewModel.cs
  Subjects/
    SubjectViewModel.cs
  StudentSubjects/
    StudentSubjectAssignmentViewModel.cs
  Schedules/
    WeeklyScheduleViewModel.cs
  Grades/
    GradeViewModels.cs        ← contains 5 classes in one file
  Announcements/
    AnnouncementViewModel.cs
  Profile/
    ProfileViewModel.cs
```

One subfolder per controller — mirrors the `Controllers/` and `Views/` folder structure. This is a convention, not a requirement, but it keeps things organized.

---

## Registration in `_ViewImports.cshtml`

All ViewModel namespaces are imported globally so views can use them without per-file `@using`:

```cshtml
@using kbu_portal
@using kbu_portal.Models
@using kbu_portal.ViewModels.Account
@using kbu_portal.ViewModels.Announcements
@using kbu_portal.ViewModels.Subjects
@using kbu_portal.ViewModels.StudentSubjects
@using kbu_portal.ViewModels.Grades
@using kbu_portal.ViewModels.Schedules
@using kbu_portal.ViewModels.Profile
@using kbu_portal.ViewModels.Admin
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

Without this, you'd need `@using kbu_portal.ViewModels.Subjects` at the top of every Subjects view.
