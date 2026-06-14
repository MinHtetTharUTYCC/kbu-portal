# Identity & Auth — KBU Portal

## What is Identity?

ASP.NET Core Identity is a **built-in authentication and authorization system** — the equivalent of **Passport.js + Auth Guards** in NestJS, but built into the framework.

It handles:
- User registration and login
- Password hashing and storage (you never see plain text passwords)
- Role-based authorization (Admin, Teacher, Student)
- Session/cookie management

---

## How It's Configured

**Program.cs lines 11-15:**
```csharp
builder.Services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>();
```

| Part | Purpose | NestJS equivalent |
|---|---|---|
| `ApplicationUser` | Custom user model with extra fields | `UserEntity` with custom fields |
| `IdentityRole` | Role model (Admin/Teacher/Student) | Role entity |
| `options.Password.RequireNonAlphanumeric = false` | Relaxed password rules for dev | Config in `@nestjs/passport` |
| `AddEntityFrameworkStores<AppDbContext>()` | Stores users in DB via EF Core | `TypeORM` or `Prisma` adapter |

**Program.cs lines 37-38 — Middleware:**
```csharp
app.UseAuthentication();   // who are you?
app.UseAuthorization();    // are you allowed to do this?
```
**Order matters.** `UseAuthentication` must come before `UseAuthorization`. Authentication identifies the user; authorization checks their permissions.

---

## The Three Roles

Seeded in `Data/SeedData.cs` lines 16-23:

```csharp
string[] roles = ["Admin", "Teacher", "Student"];
foreach (var role in roles)
{
    if (!await roleManager.RoleExistsAsync(role))
    {
        await roleManager.CreateAsync(new IdentityRole(role));
    }
}
```

| Role | Permissions | Users |
|---|---|---|
| `Admin` | Full system access — manage subjects, enrollments, view all data | `admin@kbu.local` |
| `Teacher` | Enter grades for their own subjects only | `john.smith@kbu.local`, `jane.doe@kbu.local` |
| `Student` | View own grades, schedule, announcements | `alice@`, `bob@`, `charlie@`, `diana@`, `eve@` |

---

## Key Services

### `UserManager<ApplicationUser>` — User Operations

Used in **7 of 9 controllers**. Injected via constructor:

```csharp
private readonly UserManager<ApplicationUser> _userManager;

public SubjectsController(AppDbContext db, UserManager<ApplicationUser> userManager)
{
    _db = db;
    _userManager = userManager;
}
```

**Common operations:**

```csharp
// Find by ID
var user = await _userManager.FindByIdAsync(userId);

// Find by email
var user = await _userManager.FindByEmailAsync("alice@kbu.local");

// Create user (password is hashed automatically)
var result = await _userManager.CreateAsync(user, "Password123!");
if (!result.Succeeded)
{
    foreach (var error in result.Errors)
        ModelState.AddModelError(string.Empty, error.Description);
}

// Check password
var valid = await _userManager.CheckPasswordAsync(user, "Password123!");

// Add to role
await _userManager.AddToRoleAsync(user, "Admin");

// Check roles
var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

// Get all users in a role
var teachers = await _userManager.GetUsersInRoleAsync("Teacher");

// Get current user ID
var userId = _userManager.GetUserId(User);

// Get current user object
var user = await _userManager.GetUserAsync(User);

// Update user
await _userManager.UpdateAsync(user);
```

**NestJS equivalent:** Like a `UsersService` with Prisma methods, but with built-in password hashing and role management.

### `SignInManager<ApplicationUser>` — Login/Logout

Only used in `AccountController`:

```csharp
private readonly SignInManager<ApplicationUser> _signInManager;
```

**Sign in (creates session cookie):**
```csharp
var result = await _signInManager.PasswordSignInAsync(
    model.Email,
    model.Password,
    model.RememberMe,        // persistent cookie vs session cookie
    lockoutOnFailure: false   // don't lock account on failed attempts
);

if (result.Succeeded)
{
    return RedirectToAction("Index", "Home");
}
```

**Sign out (destroys session):**
```csharp
await _signInManager.SignOutAsync();
return RedirectToAction("Index", "Home");
```

**NestJS equivalent:** `req.login()` / `req.logout()` with Passport.

---

## Authorization Patterns

### 1. Class-Level — All Actions Require Role

**SubjectsController.cs line 12:**
```csharp
[Authorize(Roles = "Admin")]
public class SubjectsController : Controller
{
    // ALL actions here require Admin role
    // No user can access any action without being an Admin
}
```

Also used on: `AdminController`, `StudentSubjectsController`

**NestJS equivalent:** `@UseGuards(RolesGuard)` + `@Roles('Admin')` at controller level.

### 2. Action-Level — Specific Actions Require Role

**GradesController.cs:**
```csharp
[Authorize]                          // any logged-in user
public class GradesController : Controller
{
    [Authorize(Roles = "Teacher")]   // only teachers
    public async Task<IActionResult> Enter(int subjectId) { ... }

    [Authorize(Roles = "Student")]   // only students
    public async Task<IActionResult> MyGrades() { ... }
}
```

### 3. Class + Action Combination

**AnnouncementsController.cs:**
```csharp
[Authorize]                          // any logged-in user
public class AnnouncementsController : Controller
{
    public IActionResult Index() { ... }   // any user can view

    [Authorize(Roles = "Admin")]           // only admins can create
    public IActionResult Create() { ... }

    [Authorize(Roles = "Admin")]           // only admins can edit
    public IActionResult Edit(int id) { ... }

    [Authorize(Roles = "Admin")]           // only admins can delete
    public IActionResult Delete(int id) { ... }
}
```

### 4. Checking Roles in Views

**_Layout.cshtml lines 29-49:**
```cshtml
@if (User.IsInRole("Admin"))
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-controller="Subjects" asp-action="Index">Subjects</a>
    </li>
}
@if (User.IsInRole("Student"))
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-controller="Grades" asp-action="MyGrades">My Grades</a>
    </li>
    <li class="nav-item">
        <a class="nav-link text-dark" asp-controller="Schedule" asp-action="Index">Schedule</a>
    </li>
}
```

### 5. Getting Current User in Controller

```csharp
// Get user ID (string)
var userId = _userManager.GetUserId(User);

// Get full user object
var user = await _userManager.GetUserAsync(User);

// Access via ClaimsPrincipal
var email = User.Identity?.Name;  // returns email (used as username)
var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
```

---

## Anti-Forgery Tokens — CSRF Protection

Every POST form includes a CSRF token:

**In Views:**
```cshtml
<form asp-action="Create" method="post">
    @Html.AntiForgeryToken()
    <!-- form fields -->
</form>
```

**In Controllers:**
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(SubjectViewModel model)
{
    // ...
}
```

**How it works:**
1. `@Html.AntiForgeryToken()` generates a hidden input with a unique token
2. The token is stored in the user's cookies AND in the form
3. On POST, `[ValidateAntiForgeryToken]` compares the cookie token with the form token
4. If they don't match → 403 Forbidden (CSRF attack blocked)

**NestJS equivalent:** CSRF middleware (like `csurf` or `@nestjs/csrf`).

---

## Login Flow — Step by Step

**AccountController.cs:**

```
1. GET /Account/Login
   → Line 28: returns View(new LoginViewModel())
   → Renders Login.cshtml with empty form

2. User fills form, submits POST /Account/Login

3. AccountController.Login(LoginViewModel model) — line 30
   → Line 34: if (!ModelState.IsValid) return View(model);
   → Line 37: var result = await _signInManager.PasswordSignInAsync(...)
   → Line 39: if (result.Succeeded)
   → Line 41: return await RedirectAfterLoginAsync();

4. RedirectAfterLoginAsync() — line 57
   → Gets current user
   → Checks roles (Admin → Dashboard, others → Home/Index)
   → Returns RedirectToAction()

5. If login fails:
   → Line 45: ModelState.AddModelError("", "Invalid login attempt.")
   → Returns view with error message
```

---

## Registration Flow — Step by Step

**AccountController.cs:**

```
1. GET /Account/Register
   → Line 19: returns View(new RegisterViewModel())
   → Renders Register.cshtml with empty form

2. User fills form, submits POST /Account/Register

3. AccountController.Register(RegisterViewModel model) — line 22
   → Line 26: if (!ModelState.IsValid) return View(model);

4. Create user — lines 29-36:
   var user = new ApplicationUser
   {
       UserName = model.Email,
       Email = model.Email,
       FirstName = model.FirstName,
       LastName = model.LastName,
       StudentId = model.StudentId,
       Faculty = model.Faculty,
       Major = model.Major
   };
   var result = await _userManager.CreateAsync(user, model.Password);

5. Check result — lines 38-43:
   if (result.Succeeded)
   {
       await _userManager.AddToRoleAsync(user, "Student");  // default role
       await _signInManager.SignInAsync(user, isPersistent: false);
       return RedirectToAction("Index", "Home");
   }

6. If creation fails:
   → Line 45: foreach (var error in result.Errors)
   → Line 47: ModelState.AddModelError(string.Empty, error.Description);
   → Returns view with errors
```

**Key detail:** Password is hashed automatically by `CreateAsync`. You never store plain text.

---

## Password Rules (Program.cs lines 11-14)

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
})
```

**Default rules (from ASP.NET Identity):**
| Rule | Default | This project |
|---|---|---|
| `RequireDigit` | true | true (default) |
| `RequireLowercase` | true | true (default) |
| `RequireUppercase` | true | true (default) |
| `RequireNonAlphanumeric` | true | **false** (relaxed) |
| `RequiredLength` | 6 | 6 (default) |

So passwords must be at least 6 characters with at least one digit, one uppercase, and one lowercase letter. No special characters required.

---

## How Auth Connects to Everything

```
Program.cs
  ├── AddIdentity<ApplicationUser, IdentityRole>()     ← configures auth system
  ├── .AddEntityFrameworkStores<AppDbContext>()          ← stores in DB
  ├── options.Password.RequireNonAlphanumeric = false    ← password rules
  ├── app.UseAuthentication()                            ← middleware: identify user
  └── app.UseAuthorization()                             ← middleware: check permissions

Controllers
  ├── [Authorize]                    ← requires login (any role)
  ├── [Authorize(Roles = "Admin")]   ← requires specific role
  ├── UserManager<T>                 ← user CRUD operations
  ├── SignInManager<T>               ← login/logout
  └── _userManager.GetUserId(User)   ← get current user ID

Views
  ├── @if (User.IsInRole("Admin"))   ← show/hide UI elements
  ├── User.Identity?.IsAuthenticated ← show login/logout buttons
  └── @Html.AntiForgeryToken()        ← CSRF protection

SeedData.cs
  ├── roleManager.CreateAsync()      ← create roles
  ├── userManager.CreateAsync()      ← create users with hashed passwords
  └── userManager.AddToRoleAsync()   ← assign roles to users
```

---

## Seed Data — Auth Setup

**SeedData.cs lines 25-42 — Admin:**
```csharp
var adminUser = new ApplicationUser
{
    UserName = adminEmail,
    Email = adminEmail,
    FirstName = "System",
    LastName = "Admin"
};
var result = await userManager.CreateAsync(adminUser, "Admin123!");
if (result.Succeeded)
{
    await userManager.AddToRoleAsync(adminUser, "Admin");
}
```

**SeedData.cs lines 44-68 — Teachers:**
```csharp
var name = email.Split('@')[0].Split('.');
teacher = new ApplicationUser
{
    UserName = email,
    Email = email,
    FirstName = name[0],   // "john"
    LastName = name[1]     // "smith"
};
var result = await userManager.CreateAsync(teacher, "Teacher123!");
await userManager.AddToRoleAsync(teacher, "Teacher");
```

**SeedData.cs lines 70-103 — Students:**
```csharp
var studentEmails = new[]
{
    ("alice@kbu.local", "Alice", "Johnson", "STU001", "Engineering", "Computer Science"),
    ("bob@kbu.local", "Bob", "Williams", "STU002", "Engineering", "Electrical"),
    // ...
};
```

**Login credentials for testing:**
| User | Email | Password | Role |
|---|---|---|---|
| Admin | admin@kbu.local | Admin123! | Admin |
| Teacher 1 | john.smith@kbu.local | Teacher123! | Teacher |
| Teacher 2 | jane.doe@kbu.local | Teacher123! | Teacher |
| Student 1 | alice@kbu.local | Student123! | Student |
| Student 2 | bob@kbu.local | Student123! | Student |
| Student 3 | charlie@kbu.local | Student123! | Student |
| Student 4 | diana@kbu.local | Student123! | Student |
| Student 5 | eve@kbu.local | Student123! | Student |

---

## Quick Reference: .NET Identity ↔ NestJS/Node.js

| .NET Identity | NestJS/Node.js |
|---|---|
| `UserManager<T>` | `UsersService` (Prisma) |
| `SignInManager<T>` | Passport `req.login()` / `req.logout()` |
| `IdentityUser` | `UserEntity` |
| `IdentityRole` | Role entity |
| `[Authorize]` | `@UseGuards(AuthGuard)` |
| `[Authorize(Roles="Admin")]` | `@UseGuards(RolesGuard)` + `@Roles('Admin')` |
| `PasswordSignInAsync()` | `passport.authenticate()` |
| `CreateAsync(user, password)` | `prisma.user.create()` + hash password |
| `AddToRoleAsync(user, "Admin")` | `prisma.userRole.create()` |
| `GetUsersInRoleAsync("Teacher")` | `prisma.user.findMany({ where: { roles: { some: { name: "Teacher" } } } })` |
| `AntiForgeryToken` | CSRF middleware |
| Cookie authentication | JWT or session cookies |
| `UseAuthentication()` | Passport initialization |
| `UseAuthorization()` | Auth guard middleware |
