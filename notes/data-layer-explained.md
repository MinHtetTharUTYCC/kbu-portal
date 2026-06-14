# Data Layer — EF Core, DbContext, Migrations, Seed Data

## What is the Data Layer?

The Data layer is your **database interface** — the equivalent of **Prisma schema + `prisma migrate` + `prisma/seed.ts`**. It handles:

- Defining what tables exist and their columns (DbContext)
- Running database migrations (Migrations folder)
- Seeding initial data (SeedData.cs)

---

## Files

| File | Purpose | Prisma Equivalent |
|---|---|---|
| `Data/AppDbContext.cs` | Database schema + relationships | `schema.prisma` |
| `Data/SeedData.cs` | Initial data population | `prisma/seed.ts` |
| `Migrations/*.cs` | Schema version history | `prisma/migrations/` |

---

## 1. `AppDbContext.cs` — Your Database Schema

**File:** `Data/AppDbContext.cs` (41 lines)

```csharp
using KbuPortal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KbuPortal.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<StudentSubject> StudentSubjects => Set<StudentSubject>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<Schedule> Schedules => Set<Schedule>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<StudentSubject>()
            .HasKey(ss => new { ss.StudentId, ss.SubjectId });

        builder.Entity<StudentSubject>()
            .HasOne(ss => ss.Student)
            .WithMany()
            .HasForeignKey(ss => ss.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudentSubject>()
            .HasOne(ss => ss.Subject)
            .WithMany(s => s.StudentSubjects)
            .HasForeignKey(ss => ss.SubjectId);

        builder.Entity<Subject>()
            .HasIndex(s => s.Code)
            .IsUnique();
    }
}
```

### Key Concepts

**`IdentityDbContext<ApplicationUser>` (line 7)**
- Inherits from Identity's DbContext, which creates all the ASP.NET Identity tables automatically (AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, etc.)
- Your custom tables (Subjects, Grades, etc.) are added on top via `DbSet<T>`
- Without this inheritance, you'd have to manually define the Identity tables

**`DbSet<T>` = Table (lines 13-17)**
```csharp
public DbSet<Subject> Subjects => Set<Subject>();
```
Each `DbSet<T>` represents a database table. The `=> Set<Subject>()` syntax is a C# expression-bodied property that creates a new `DbSet` instance.

**Prisma mapping:**
| .NET | Prisma |
|---|---|
| `DbSet<Subject>` | `model Subject { ... }` |
| `OnModelCreating` | Relations in schema |
| `DbContext` | `PrismaClient` (conceptually) |

---

### Relationship Configuration — Fluent API (lines 23-39)

The `OnModelCreating` method configures relationships using the **Fluent API**. This is where you define things that can't be expressed with just Model properties.

**Composite Primary Key (lines 23-24):**
```csharp
builder.Entity<StudentSubject>()
    .HasKey(ss => new { ss.StudentId, ss.SubjectId });
```
Creates a composite primary key from two columns. Prisma equivalent: `@@id([studentId, subjectId])`.

**One-to-Many with Restrict Delete (lines 26-30):**
```csharp
builder.Entity<StudentSubject>()
    .HasOne(ss => ss.Student)       // each StudentSubject has one Student
    .WithMany()                      // a Student has many StudentSubjects (no nav prop on other side)
    .HasForeignKey(ss => ss.StudentId)
    .OnDelete(DeleteBehavior.Restrict);  // prevent cascade delete
```
`DeleteBehavior.Restrict` means: you can't delete a Student who has enrollments — you must delete the enrollments first. This prevents accidental data loss.

Prisma equivalent: `student Student @relation(fields: [studentId], references: [id])` — Prisma uses cascade by default, so you'd need `onDelete: Restrict`.

**One-to-Many with Navigation (lines 32-35):**
```csharp
builder.Entity<StudentSubject>()
    .HasOne(ss => ss.Subject)
    .WithMany(s => s.StudentSubjects)  // Subject has a nav property
    .HasForeignKey(ss => ss.SubjectId);
```
This connects to `Subject.StudentSubjects` (the `ICollection<StudentSubject>` in the Model).

**Unique Index (lines 37-39):**
```csharp
builder.Entity<Subject>()
    .HasIndex(s => s.Code)
    .IsUnique();
```
Ensures no two subjects can have the same code. Prisma equivalent: `@@unique([code])`.

---

## 2. Navigation Properties — Loading Related Data

In your Models, navigation properties let you load related data. Here's how each Model connects:

```csharp
// Subject.cs — has many StudentSubjects and Grades
public ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();
public ICollection<Grade> Grades { get; set; } = new List<Grade>();

// Subject.cs — belongs to a Teacher (nullable)
public string? TeacherId { get; set; }
public ApplicationUser? Teacher { get; set; }

// Grade.cs — belongs to Student and Subject
public string StudentId { get; set; }
public ApplicationUser Student { get; set; } = null!;
public int SubjectId { get; set; }
public Subject Subject { get; set; } = null!;
```

### How to query with related data:

**Eager loading with `.Include()` (like Prisma's `include`):**
```csharp
// Load a subject with its teacher and enrollments
var subject = await _db.Subjects
    .Include(s => s.Teacher)           // JOIN AspNetUsers ON TeacherId
    .Include(s => s.StudentSubjects)   // JOIN StudentSubjects ON SubjectId
    .FirstOrDefaultAsync(s => s.Id == 1);
```
Prisma equivalent:
```javascript
prisma.subject.findUnique({
  where: { id: 1 },
  include: { teacher: true, studentSubjects: true }
})
```

**Projection with `.Select()` (like Prisma's `select`):**
```csharp
// Load only the fields the view needs
var subjects = await _db.Subjects
    .AsNoTracking()
    .Include(s => s.Teacher)
    .Select(s => new SubjectViewModel
    {
        Id = s.Id,
        Name = s.Name,
        Code = s.Code,
        Credits = s.Credits,
        TeacherId = s.TeacherId,
        TeacherName = s.Teacher != null
            ? (s.Teacher.FirstName ?? string.Empty) + " " + (s.Teacher.LastName ?? string.Empty)
            : "Unassigned"
    })
    .OrderBy(s => s.Code)
    .ToListAsync();
```
This is the exact code from `SubjectsController.Index()` (lines 27-42). It projects directly into the ViewModel — the DB only returns the needed columns.

**Filtering with `.Where()` (like Prisma's `where`):**
```csharp
var students = await _db.Users
    .Where(u => u.Email.Contains("gmail"))
    .ToListAsync();
```

**Existence check with `.Any()` (like Prisma's `findFirst` or `count`):**
```csharp
var exists = await _db.Subjects.AnyAsync(s => s.Code == model.Code);
if (exists)
{
    ModelState.AddModelError(nameof(model.Code), "Subject code already exists.");
}
```

---

## 3. `SeedData.cs` — Initial Data (291 lines)

**File:** `Data/SeedData.cs`

Equivalent to `prisma/seed.ts`. Runs at startup to populate the database.

### How it's called (Program.cs line 47):
```csharp
await SeedData.InitializeAsync(app.Services);
```

### The seeding process — step by step:

**Step 1: Get services (lines 10-13):**
```csharp
using var scope = serviceProvider.CreateScope();
var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
```
Creates a DI scope to resolve services. `GetRequiredService` throws if the service isn't registered.

**Step 2: Create roles (lines 15-23):**
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
Creates 3 roles. Idempotent — checks if they exist first.

**Step 3: Seed Admin user (lines 25-42):**
```csharp
var adminEmail = "admin@kbu.local";
var adminUser = await userManager.FindByEmailAsync(adminEmail);
if (adminUser == null)
{
    adminUser = new ApplicationUser
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
}
```
Key pattern: `CreateAsync(user, password)` hashes the password automatically. You never store plain text.

**Step 4: Seed Teachers (lines 44-68):**
```csharp
var teacherEmails = new[] { "john.smith@kbu.local", "jane.doe@kbu.local" };
foreach (var email in teacherEmails)
{
    var teacher = await userManager.FindByEmailAsync(email);
    if (teacher == null)
    {
        var name = email.Split('@')[0].Split('.');
        teacher = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = name[0],
            LastName = name[1]
        };
        var result = await userManager.CreateAsync(teacher, "Teacher123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(teacher, "Teacher");
        }
    }
    teachers.Add(teacher);
}
```
Clever: splits `"john.smith"` on `.` to get `FirstName = "john"`, `LastName = "smith"`.

**Step 5: Seed Students (lines 70-103):**
```csharp
var studentEmails = new[]
{
    ("alice@kbu.local", "Alice", "Johnson", "STU001", "Engineering", "Computer Science"),
    ("bob@kbu.local", "Bob", "Williams", "STU002", "Engineering", "Electrical"),
    // ... 5 students total
};
foreach (var (email, first, last, stuId, faculty, major) in studentEmails)
{
    // ... creates each student with all fields
}
```
Uses C# tuple deconstruction for clean iteration.

**Step 6: Seed Subjects (lines 105-138):**
```csharp
if (!db.Subjects.Any())
{
    var subjects = new[]
    {
        new Subject { Code = "CS101", Name = "Introduction to Programming", Credits = 3, TeacherId = teachers[0].Id },
        new Subject { Code = "CS201", Name = "Data Structures", Credits = 4, TeacherId = teachers[1].Id },
        new Subject { Code = "MATH101", Name = "Calculus I", Credits = 4, TeacherId = teachers[0].Id }
    };
    foreach (var subject in subjects)
        db.Subjects.Add(subject);
    await db.SaveChangesAsync();
}
```
`db.Subjects.Any()` — checks if subjects already exist. If yes, skip seeding.

**Step 7: Seed Enrollments (lines 140-156):**
```csharp
if (!db.StudentSubjects.Any())
{
    var subjects = db.Subjects.ToList();
    foreach (var student in students)
    {
        foreach (var subject in subjects.Take(2))  // first 2 subjects only
        {
            db.StudentSubjects.Add(new StudentSubject
            {
                StudentId = student.Id,
                SubjectId = subject.Id
            });
        }
    }
    await db.SaveChangesAsync();
}
```
Each student gets enrolled in the first 2 subjects (CS101 and CS201).

**Step 8: Seed Grades (lines 158-192):**
```csharp
if (!db.Grades.Any())
{
    var grades = new[]
    {
        (studentId: students[0].Id, subjectId: subjects[0].Id, score: 92m, semester: 1),
        (studentId: students[0].Id, subjectId: subjects[1].Id, score: 88m, semester: 1),
        // ... 6 grades total
    };
    foreach (var (studentId, subjectId, score, semester) in grades)
    {
        var letterGrade = score >= 90 ? "A"
            : score >= 80 ? "B"
            : score >= 70 ? "C"
            : score >= 60 ? "D"
            : "F";
        db.Grades.Add(new Grade { ... });
    }
    await db.SaveChangesAsync();
}
```
Letter grade is computed inline using a ternary chain.

**Step 9: Seed Schedules (lines 194-247):**
```csharp
if (!db.Schedules.Any())
{
    var schedules = new[]
    {
        new Schedule { SubjectId = subjects[0].Id, DayOfWeek = DayOfWeek.Monday,
                       StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0),
                       Room = "A101" },
        // ... 5 schedules total
    };
}
```
`TimeSpan(9, 0, 0)` = 9:00 AM. `DayOfWeek.Monday` = .NET enum.

**Step 10: Seed Announcements (lines 249-288):**
```csharp
if (!db.Announcements.Any())
{
    var announcements = new[]
    {
        new Announcement
        {
            Title = "Welcome to KBU Portal",
            Content = "Welcome to the Kasem Bundit University student portal...",
            CreatedAt = DateTime.UtcNow,
            CreatedById = adminUser.Id,
            IsPinned = true,
            TargetRole = "All"
        },
        // ... 3 announcements
    };
}
```

---

## 4. Migrations — Schema Version History

**Folder:** `Migrations/`

### Current state:
- **1 migration:** `InitialCreate` (2026-06-05)
- Creates all 12 tables (5 custom + 7 Identity tables)

### Tables created:

| Table | Source | Key columns |
|---|---|---|
| `AspNetUsers` | Identity + custom | Id, Email, FirstName, LastName, StudentId, Faculty, Major, ProfilePhoto |
| `AspNetRoles` | Identity | Id, Name |
| `AspNetUserRoles` | Identity (join) | UserId, RoleId |
| `AspNetUserClaims` | Identity | Id, UserId |
| `AspNetRoleClaims` | Identity | Id, RoleId |
| `AspNetUserLogins` | Identity | LoginProvider, ProviderKey, UserId |
| `AspNetUserTokens` | Identity | UserId, LoginProvider, Name |
| `Subjects` | Custom | Id, Name, Code (unique), Credits, TeacherId |
| `StudentSubjects` | Custom (join) | StudentId + SubjectId (composite PK) |
| `Grades` | Custom | Id, StudentId, SubjectId, Score, LetterGrade, Semester, Year |
| `Schedules` | Custom | Id, SubjectId, DayOfWeek, StartTime, EndTime, Room |
| `Announcements` | Custom | Id, Title, Content, CreatedAt, CreatedById, IsPinned, TargetRole |

### How to create a new migration:

```bash
# Add a migration
dotnet ef migrations add MigrationName

# Apply to database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

Prisma equivalent: `prisma migrate dev --name MigrationName`

---

## 5. Querying the Database — CRUD Operations

### How controllers access the DB:

**Injection (constructor):**
```csharp
public class SubjectsController : Controller
{
    private readonly AppDbContext _db;

    public SubjectsController(AppDbContext db)
    {
        _db = db;
    }
}
```

**Read — Find one (line 143 of SubjectsController):**
```csharp
var subject = await _db.Subjects.FindAsync(id);
```
Prisma: `prisma.subject.findUnique({ where: { id } })`

**Read — List all with related data (lines 27-42 of SubjectsController):**
```csharp
var subjects = await _db.Subjects
    .AsNoTracking()                    // don't track changes (read-only)
    .Include(s => s.Teacher)           // eager load teacher
    .Select(s => new SubjectViewModel  // project into ViewModel
    {
        Id = s.Id,
        Name = s.Name,
        Code = s.Code,
        Credits = s.Credits,
        TeacherId = s.TeacherId,
        TeacherName = s.Teacher != null
            ? (s.Teacher.FirstName ?? string.Empty) + " " + (s.Teacher.LastName ?? string.Empty)
            : "Unassigned"
    })
    .OrderBy(s => s.Code)
    .ToListAsync();
```
Prisma: `prisma.subject.findMany({ include: { teacher: true }, orderBy: { code: 'asc' } })`

**Read — Check existence (line 77):**
```csharp
var exists = await _db.Subjects.AnyAsync(s => s.Code == model.Code);
```
Prisma: `await prisma.subject.findFirst({ where: { code } }) !== null`

**Create (lines 85-94):**
```csharp
var subject = new Subject
{
    Name = model.Name,
    Code = model.Code,
    Credits = model.Credits,
    TeacherId = string.IsNullOrWhiteSpace(model.TeacherId) ? null : model.TeacherId
};
_db.Subjects.Add(subject);
await _db.SaveChangesAsync();
```
Prisma: `await prisma.subject.create({ data: { ... } })`

**Update (lines 173-178):**
```csharp
subject.Name = model.Name;
subject.Code = model.Code;
subject.Credits = model.Credits;
subject.TeacherId = string.IsNullOrWhiteSpace(model.TeacherId) ? null : model.TeacherId;
await _db.SaveChangesAsync();
```
Prisma: `await prisma.subject.update({ where: { id }, data: { ... } })`

**Delete (lines 194-195):**
```csharp
_db.Subjects.Remove(subject);
await _db.SaveChangesAsync();
```
Prisma: `await prisma.subject.delete({ where: { id } })`

---

## 6. Auto-Migration on Startup

**Program.cs lines 20-24:**
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
```

This runs all pending migrations automatically when the app starts. Equivalent to `prisma migrate deploy` on startup.

---

## 7. Database Configuration

**Program.cs lines 9-10:**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=KBUPortal.db"
  }
}
```

SQLite is used for this learning project — single file, no server needed. For production, you'd swap to SQL Server (the package is already included in `.csproj`).

---

## Quick Reference: .NET ↔ Prisma

| .NET EF Core | Prisma |
|---|---|
| `AppDbContext` | `schema.prisma` |
| `DbSet<T>` | `model T { }` |
| `OnModelCreating` | Relations in schema |
| Fluent API | `@relation()` attributes |
| `Migration` | `prisma migrate` |
| `SeedData` | `prisma/seed.ts` |
| `.Include(x => x.Relation)` | `include: { relation: true }` |
| `.Where(x => ...)` | `where: { ... }` |
| `.Select(x => new { ... })` | `select: { ... }` |
| `.AsNoTracking()` | N/A (Prisma doesn't cache) |
| `FindAsync(id)` | `findUnique({ where: { id } })` |
| `ToListAsync()` | `findMany()` |
| `AnyAsync()` | `findFirst()` or `count()` |
| `Add()` + `SaveChangesAsync()` | `create()` |
| `Remove()` + `SaveChangesAsync()` | `delete()` |
| `SaveChangesAsync()` | Auto-commit on mutations |
