# Controllers Folder — KBU Portal

## What are Controllers?

In ASP.NET Core MVC, Controllers handle **incoming HTTP requests** and return responses (usually Views). They are the equivalent of **NestJS `@Controller()` classes**.

Each controller:
- Receives a request (GET, POST, etc.)
- Accesses the database (via `AppDbContext` or `UserManager`)
- Returns a View (HTML page) or redirects

---

## Routing

In NestJS you use decorators like `@Get('subjects')`. In .NET, routing is configured in `Program.cs`:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

This means a URL like `/Subjects/Edit/3` maps to:
- Controller: `SubjectsController`
- Action: `Edit`
- Parameter: `id = 3`

You can also use `[Route]` and `[HttpGet]` attributes for explicit routing, but this project uses convention-based routing.

---

## Dependency Injection (DI)

In NestJS, you inject services via constructor parameters with `@Injectable()`. In .NET, it works the same way — services are registered in `Program.cs` and injected into controllers via constructor:

```csharp
public class SubjectsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubjectsController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }
}
```

The framework creates the controller and automatically provides the registered services. No decorators needed on the parameters.

---

## The 9 Controllers

### 1. `HomeController.cs` — Public Pages
**File:** `Controllers/HomeController.cs`

| | |
|---|---|
| **Auth** | None — fully public |
| **Actions** | `Index()`, `Privacy()`, `About()`, `Error()` |
| **DB access** | None |

Simplest controller. Just returns Views. No dependencies injected. Equivalent to a NestJS controller with no guards and no service injection.

---

### 2. `AccountController.cs` — Login / Register / Logout
**File:** `Controllers/AccountController.cs`

| | |
|---|---|
| **Auth** | None on actions (public endpoints) |
| **Actions** | `Register()` [GET+POST], `Login()` [GET+POST], `Logout()` [POST] |
| **DI** | `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>` |

**Key patterns:**
- `[ValidateAntiForgeryToken]` on every POST — CSRF protection
- `RedirectAfterLoginAsync()` — checks user role after login and redirects accordingly
- `IsLocalUrl()` — prevents open redirect attacks
- Assigns "Student" role to new registrations by default

**NestJS equivalent:** Like an auth module with `@Post('login')`, `@Post('register')`, using `AuthGuard`.

---

### 3. `AdminController.cs` — Dashboard Stats
**File:** `Controllers/AdminController.cs`

| | |
|---|---|
| **Auth** | `[Authorize(Roles = "Admin")]` — admin only |
| **Actions** | `Dashboard()` [GET] |
| **DI** | `AppDbContext`, `UserManager<ApplicationUser>` |

**Key patterns:**
- `.AsNoTracking()` — performance optimization for read-only queries
- LINQ `.Select()` projection — fetches only needed fields from DB
- Uses `ViewBag` for passing data to the view
- Returns `AdminDashboardViewModel` with aggregate stats (counts, recent announcements, top subjects)

---

### 4. `SubjectsController.cs` — Full CRUD
**File:** `Controllers/SubjectsController.cs`

| | |
|---|---|
| **Auth** | `[Authorize(Roles = "Admin")]` — admin only |
| **Actions** | `Index()` [GET], `Create()` [GET+POST], `Edit()` [GET+POST], `Delete()` [POST] |
| **DI** | `AppDbContext`, `UserManager<ApplicationUser>` |

**Key patterns:**
- Full CRUD (Create, Read, Update, Delete)
- `PopulateTeacherDropdown()` — builds `SelectList` for teacher dropdown using `ViewBag.Teachers`
- Validates unique subject code before create/edit
- Validates teacher exists and has "Teacher" role
- PRG pattern: POST → redirect to GET (prevents duplicate form submission)
- `TempData["StatusMessage"]` — flash message after successful operation

**NestJS equivalent:** Like a CRUD controller with `@Get()`, `@Post()`, `@Put()`, `@Delete()` and input validation.

---

### 5. `GradesController.cs` — Teacher Enter / Student View
**File:** `Controllers/GradesController.cs`

| | |
|---|---|
| **Auth** | `[Authorize]` at class level + action-level role checks |
| **Actions** | `Enter()` [GET+POST] (Teacher), `MyGrades()` [GET] (Student) |
| **DI** | `AppDbContext`, `UserManager<ApplicationUser>` |

**Key patterns:**
- Role-scoped actions: Teachers can only enter grades for subjects they teach; Students can only see their own grades
- Upsert pattern: updates existing grade or creates new one
- Auto-calculates letter grade from score (A=90+, B=80+, etc.)
- Auto-detects current semester from date (month >= 6 = semester 2)
- Calculates GPA per semester using credit-weighted average
- Uses `@for` loop with indexed model binding for form arrays

**NestJS equivalent:** Like having `@UseGuards(RolesGuard)` on specific routes, with ownership checks in the service layer.

---

### 6. `AnnouncementsController.cs` — CRUD with Role Filtering
**File:** `Controllers/AnnouncementsController.cs`

| | |
|---|---|
| **Auth** | `[Authorize]` at class level; Create/Edit/Delete require `"Admin"` role |
| **Actions** | `Index()` [GET], `Create()` [GET+POST], `Edit()` [GET+POST], `Delete()` [POST] |
| **DI** | `AppDbContext`, `UserManager<ApplicationUser>` |

**Key patterns:**
- Role-based content filtering at query level:
  - Students see announcements with `TargetRole` of "Student" or "All"
  - Teachers see "Teacher" or "All"
  - Admins see everything
- Orders by `IsPinned` descending, then by date
- Sets `CreatedById` to current user on create

---

### 7. `ScheduleController.cs` — Student Weekly Schedule
**File:** `Controllers/ScheduleController.cs`

| | |
|---|---|
| **Auth** | `[Authorize]` + `"Student"` role required |
| **Actions** | `Index()` [GET] |
| **DI** | `AppDbContext`, `UserManager<ApplicationUser>` |

**Key patterns:**
- Read-only, single action
- Loads all schedules with `.Include(s => s.Subject)` for related data
- Filters in-memory: only shows schedules for subjects the student is enrolled in
- Orders by `DayOfWeek` then `StartTime`

---

### 8. `ProfileController.cs` — User Profile + Photo Upload
**File:** `Controllers/ProfileController.cs`

| | |
|---|---|
| **Auth** | `[Authorize]` — any logged-in user |
| **Actions** | `Index()` [GET], `Update()` [POST] |
| **DI** | `UserManager<ApplicationUser>`, `IWebHostEnvironment` |

**Key patterns:**
- `IFormFile` — .NET's way of handling file uploads (equivalent to `multer` in Node.js)
- Validates file extension (JPG/PNG/GIF/WebP) and size (max 2MB)
- Saves to `wwwroot/uploads/` with unique filename (`{userId}_{guid}.ext`)
- Uses `IWebHostEnvironment.WebRootPath` to get the physical wwwroot path
- Single-view pattern: `Index.cshtml` serves as both display and edit form

---

### 9. `StudentSubjectsController.cs` — Enrollment Management
**File:** `Controllers/StudentSubjectsController.cs`

| | |
|---|---|
| **Auth** | `[Authorize(Roles = "Admin")]` — admin only |
| **Actions** | `AssignStudents()` [GET+POST] |
| **DI** | `AppDbContext`, `UserManager<ApplicationUser>` |

**Key patterns:**
- Checkbox-based bulk assignment
- Full diff approach: removes all current enrollments for a subject, then adds back the selected ones
- Uses `StudentSubjectAssignmentViewModel` with `IsAssigned` flag per student

---

## Cross-Cutting Patterns

| Pattern | What it is | NestJS equivalent |
|---|---|---|
| `[Authorize]` | Requires login | `@UseGuards(AuthGuard)` |
| `[Authorize(Roles = "Admin")]` | Requires specific role | `@UseGuards(RolesGuard)` + `@Roles('Admin')` |
| `[ValidateAntiForgeryToken]` | CSRF protection on POST | CSRF middleware |
| `TempData["StatusMessage"]` | Flash message after redirect | Session flash / redirect flash |
| PRG (Post-Redirect-Get) | POST → redirect to GET | `return res.redirect()` |
| `.AsNoTracking()` | Read-only query optimization | N/A (Prisma doesn't cache) |
| `UserManager<T>` | User CRUD operations | Prisma user model methods |
| `SignInManager<T>` | Login/logout/session | Passport.js / auth service |
| `ViewBag` | Pass dynamic data to view | Template locals |
| `ModelState.IsValid` | Server-side validation | class-validator in NestJS |

---

## How a Request Flows

```
Browser → GET /Subjects
  ↓
Program.cs routes to SubjectsController.Index()
  ↓
SubjectsController queries AppDbContext
  ↓
Returns View("Index", viewModel) → Razor renders HTML
  ↓
Browser receives HTML page
```

For POST:
```
Browser → POST /Subjects/Create (with form data + anti-forgery token)
  ↓
SubjectsController.Create(SubjectViewModel model)
  ↓
if (ModelState.IsValid) → save to DB → redirect to GET /Subjects
else → return View with validation errors
```
