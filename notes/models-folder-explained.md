# Models Folder — KBU Portal

## What is the `Models/` folder?

In ASP.NET Core, the `Models` folder holds your **data classes** — the equivalent of a **Prisma schema**. Each `.cs` file defines a shape of data that maps to a database table.

---

## The 7 Models

### 1. `ApplicationUser.cs` — extends Identity's built-in user

```csharp
public class ApplicationUser : IdentityUser
```

- Inherits from `IdentityUser` (gives you `Id`, `Email`, `PasswordHash`, etc. for free)
- Adds custom fields: `FirstName`, `LastName`, `StudentId`, `Faculty`, `Major`, `ProfilePhoto`
- **NestJS analogy**: Like extending a base `UserEntity` with extra columns

### 2. `Subject.cs` — a course/class

- `Id`, `Name`, `Code`, `Credits`
- `TeacherId` → belongs to an `ApplicationUser` (the teacher)
- Has collections: `StudentSubjects` (enrolled students), `Grades`
- **Prisma analogy**: `Subject` model with `@@unique([code])` and relations

### 3. `StudentSubject.cs` — **join table** (many-to-many)

- Just two FKs: `StudentId` + `SubjectId`
- No `Id` property — composite key is configured in `AppDbContext`
- **Prisma analogy**: An implicit many-to-many join table

### 4. `Grade.cs` — a student's grade in a subject

- `StudentId` → FK to `ApplicationUser`
- `SubjectId` → FK to `Subject`
- `Score`, `LetterGrade`, `Semester`, `Year`

### 5. `Schedule.cs` — class schedule for a subject

- `SubjectId` → FK to `Subject`
- `DayOfWeek` (enum), `StartTime`, `EndTime` (TimeSpan)
- `Room`

### 6. `Announcement.cs` — posted announcements

- `Title`, `Content`, `CreatedAt`
- `CreatedById` → FK to `ApplicationUser` (who posted it)
- `IsPinned`, `TargetRole`

### 7. `ErrorViewModel.cs` — error page helper (not a DB entity)

---

## Relationship Map

```
ApplicationUser (Student/Teacher)
  ├── Subject (as Teacher via TeacherId)
  ├── StudentSubject ── Subject  (many-to-many)
  ├── Grade ── Subject
  └── Announcement (as CreatedBy)

Subject
  ├── StudentSubject ── ApplicationUser (students)
  ├── Grade ── ApplicationUser (students)
  ├── Schedule
  └── Teacher → ApplicationUser
```

---

## .NET / EF Core vs Prisma (Mapping Cheat Sheet)

| .NET / EF Core | Prisma equivalent |
|---|---|
| `Model` class | `model` in schema.prisma |
| `DbSet<T>` in DbContext | `prisma.subject.findMany()` — table access |
| Navigation property (`Subject Subject`) | `include: { subject: true }` |
| `[Required]` attribute | `@required` |
| `= null!` / `= string.Empty` | Prisma handles this via `?` + defaults |
| Composite key in `OnModelCreating` | `@@id([studentId, subjectId])` |

> `= null!` is C#'s way of saying "this won't actually be null, but the compiler doesn't know that yet" — think of it as a non-null assertion (`!`) baked into initialization.

---

## Registration in DbContext

All models are registered as `DbSet<T>` in `Data/AppDbContext.cs` — this is your equivalent of the Prisma schema telling Prisma "these tables exist."

The `AppDbContext` extends `IdentityDbContext<ApplicationUser>`, which handles all the ASP.NET Identity tables (users, roles, claims, etc.) plus your custom entities.
