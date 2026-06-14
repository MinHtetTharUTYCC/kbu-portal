# Program.cs — App Startup & Middleware — KBU Portal

## What is Program.cs?

`Program.cs` is the **entry point** of the entire application — the equivalent of **NestJS `main.ts`**. Everything starts here: services are registered, middleware is configured, the database is initialized, and the app starts listening for requests.

**File:** `Program.cs` (50 lines)

---

## Line-by-Line Walkthrough

### Lines 1-3 — Imports
```csharp
using KbuPortal.Data;
using KbuPortal.Models;
using Microsoft.EntityFrameworkCore;
```
Imports the namespaces needed: `AppDbContext`, `ApplicationUser`, and EF Core.

### Lines 5-6 — Create Builder
```csharp
var builder = WebApplication.CreateBuilder(args);
```
Creates a `WebApplicationBuilder` — the .NET equivalent of NestJS's module system. This is where you register all services.

**NestJS equivalent:** Creating the `AppModule` with `@Module({})`.

### Lines 7-8 — Register MVC
```csharp
builder.Services.AddControllersWithViews();
```
Registers the MVC framework — controllers, views, model binding, validation, etc.

**NestJS equivalent:** `@nestjs/platform-express` with default adapters.

### Lines 9-10 — Register Database
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```
Registers `AppDbContext` as a scoped service (new instance per request). Uses SQLite with connection string from `appsettings.json`.

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=KBUPortal.db"
  }
}
```

**NestJS equivalent:** `TypeOrmModule.forRoot({...})` or `PrismaModule`.

### Lines 11-15 — Register Identity (Auth)
```csharp
builder.Services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>();
```
Registers the Identity system:
- `ApplicationUser` — your custom user model
- `IdentityRole` — role model (Admin, Teacher, Student)
- `options.Password.RequireNonAlphanumeric = false` — relaxed password rules
- `AddEntityFrameworkStores<AppDbContext>()` — stores users in your SQLite database

**NestJS equivalent:** `@nestjs/passport` with `AuthModule.register({...})`.

### Line 17 — Build App
```csharp
var app = builder.Build();
```
Builds the app from the configured builder. After this line, you can't register more services.

### Lines 19-24 — Auto-Migration
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
```
Creates a DI scope, resolves `AppDbContext`, and runs all pending migrations. This ensures the database schema is up to date on every startup.

**NestJS equivalent:** `prisma migrate deploy` called in `main()`.

### Lines 26-32 — Exception Handling + HSTS
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
```
- `UseExceptionHandler` — catches unhandled exceptions and shows the Error page
- `UseHsts` — HTTP Strict Transport Security (forces HTTPS in production)

Only applies in non-development mode.

### Lines 34-35 — HTTPS + Routing
```csharp
app.UseHttpsRedirection();
app.UseRouting();
```
- `UseHttpsRedirection` — redirects HTTP to HTTPS
- `UseRouting` — enables endpoint routing (matches URLs to controllers)

### Lines 37-38 — Authentication + Authorization
```csharp
app.UseAuthentication();
app.UseAuthorization();
```
**Order matters.** Authentication (who are you?) must come before Authorization (are you allowed?).

These are **middleware** — they run on every request. `UseAuthentication` reads the session cookie and populates `User`. `UseAuthorization` checks `[Authorize]` attributes.

### Line 40 — Static Files
```csharp
app.MapStaticAssets();
```
Serves static files from `wwwroot/` (CSS, JS, images).

### Lines 42-45 — Route Configuration
```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
```
Defines the default route pattern:
- `{controller=Home}` — defaults to `HomeController`
- `{action=Index}` — defaults to `Index` action
- `{id?}` — optional ID parameter

So `/Subjects/Edit/3` maps to `SubjectsController.Edit(3)`.

**NestJS equivalent:** `app.setGlobalPrefix('')` + controller decorators.

### Line 47 — Seed Data
```csharp
await SeedData.InitializeAsync(app.Services);
```
Runs the seed data script — creates roles, users, subjects, grades, schedules, and announcements.

### Line 50 — Start Listening
```csharp
app.Run();
```
Starts the HTTP server and begins listening for requests.

---

## The Middleware Pipeline — Request Flow

```
Request comes in
  ↓
UseExceptionHandler (catch errors)
  ↓
UseHsts (force HTTPS in prod)
  ↓
UseHttpsRedirection (HTTP → HTTPS)
  ↓
UseRouting (match URL to controller)
  ↓
UseAuthentication (read cookie, identify user)
  ↓
UseAuthorization (check [Authorize] attributes)
  ↓
MapStaticAssets (serve CSS/JS/images)
  ↓
MapControllerRoute (execute controller action)
  ↓
Response goes out
```

**NestJS equivalent:** This is like NestJS's middleware pipeline, but explicit. In NestJS, middleware is configured per-module; here, it's a linear pipeline.

---

## Dependency Injection — How Services Get Into Controllers

**Registration (Program.cs):**
```csharp
builder.Services.AddDbContext<AppDbContext>(...);       // scoped
builder.Services.AddIdentity<...>();                     // scoped
builder.Services.AddControllersWithViews();              // transient/scoped
```

**Injection (Controller):**
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

The framework sees the constructor parameters, looks up the registered services, and injects them automatically. No decorators needed on the parameters.

**NestJS equivalent:** `@Injectable()` services registered in a module, injected via constructor with `@Inject()`.

**Service lifetimes:**
| Lifetime | Meaning | Example |
|---|---|---|
| `Scoped` | One instance per request | `AppDbContext`, `UserManager` |
| `Singleton` | One instance for the app lifetime | Configuration, logging |
| `Transient` | New instance every time it's requested | Controllers |

---

## How the App Starts — Sequence

```
1. builder = WebApplication.CreateBuilder(args)
2. Register services (AddDbContext, AddIdentity, AddControllersWithViews)
3. app = builder.Build()
4. Run migrations (db.Database.MigrateAsync())
5. Configure middleware pipeline
6. Seed data (SeedData.InitializeAsync)
7. app.Run() — start listening
```

---

## Quick Reference: .NET Program.cs ↔ NestJS

| .NET Program.cs | NestJS main.ts |
|---|---|
| `WebApplication.CreateBuilder()` | `NestFactory.create(AppModule)` |
| `builder.Services.AddDbContext()` | `TypeOrmModule.forRoot()` |
| `builder.Services.AddIdentity()` | `AuthModule.register()` |
| `builder.Services.AddControllersWithViews()` | `@nestjs/platform-express` |
| `app.UseAuthentication()` | Passport initialization |
| `app.UseAuthorization()` | Auth guards |
| `app.UseRouting()` | Built-in (automatic) |
| `app.MapControllerRoute()` | `app.setGlobalPrefix()` |
| `app.MapStaticAssets()` | `ServeStaticModule` |
| `await db.Database.MigrateAsync()` | `prisma migrate deploy` |
| `await SeedData.InitializeAsync()` | `prisma/seed.ts` |
| `app.Run()` | `app.listen(3000)` |
