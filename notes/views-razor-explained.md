# Views Folder + Razor Syntax — KBU Portal

## What are Views?

Views are **server-side rendered HTML templates** with embedded C# code. They are the equivalent of **EJS/Handlebars templates** in NestJS, but far more powerful because they use a full programming language (C#) instead of a logic-limited template syntax.

**NestJS equivalent:** Like `.hbs` (Handlebars) or `.ejs` templates, but with C# instead of JavaScript.

---

## Folder Structure

```
Views/
  _ViewImports.cshtml              ← global imports (namespaces + tag helpers)
  _ViewStart.cshtml                ← sets default layout for all views
  Shared/
    _Layout.cshtml                 ← master layout (navbar, footer, scripts)
    _ValidationScriptsPartial.cshtml  ← jQuery validation scripts
    Error.cshtml                   ← error page
  Account/
    Login.cshtml
    Register.cshtml
  Admin/
    Dashboard.cshtml
  Announcements/
    Index.cshtml, Create.cshtml, Edit.cshtml
  Grades/
    Enter.cshtml, MyGrades.cshtml
  Home/
    Index.cshtml, About.cshtml, Privacy.cshtml
  Profile/
    Index.cshtml
  Schedule/
    Index.cshtml
  StudentSubjects/
    AssignStudents.cshtml
  Subjects/
    Index.cshtml, Create.cshtml, Edit.cshtml
```

**22 .cshtml files total.** Folder-per-controller convention — matches `Controllers/` and `ViewModels/`.

---

## Key Structural Files

### `_ViewImports.cshtml` — Global Imports (11 lines)

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

Two things happening here:
1. **`@using`** — imports C# namespaces so you can reference types like `SubjectViewModel` in any view without adding `@using` per file
2. **`@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`** — registers all built-in Tag Helpers (`asp-for`, `asp-action`, `asp-route-*`, etc.). Without this line, Tag Helpers are just treated as plain HTML attributes.

### `_ViewStart.cshtml` — Default Layout (3 lines)

```cshtml
@{
    Layout = "_Layout";
}
```

Every view automatically uses `_Layout.cshtml` as its master template. You don't need to specify this in each view — it's inherited.

### `_Layout.cshtml` — Master Layout (108 lines)

This is the "shell" that wraps every page. Key sections:

**Line 6 — Dynamic title:**
```cshtml
<title>@ViewData["Title"] - KBU Portal</title>
```
Each view sets `ViewData["Title"]` and it appears in the browser tab.

**Lines 8-10 — CSS loading:**
```cshtml
<link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
<link rel="stylesheet" href="~/kbu_portal.styles.css" asp-append-version="true" />
```
`~/` resolves to the app's root URL. `asp-append-version="true"` adds `?v=hash` for cache busting.

**Lines 29-49 — Role-based navigation:**
```cshtml
@if (User.IsInRole("Admin"))
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="" asp-controller="Subjects" asp-action="Index">Subjects</a>
    </li>
}
@if (User.IsInRole("Student"))
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="" asp-controller="Grades" asp-action="MyGrades">My Grades</a>
    </li>
    <li class="nav-item">
        <a class="nav-link text-dark" asp-area="" asp-controller="Schedule" asp-action="Index">Schedule</a>
    </li>
}
```
This is how the navbar changes based on who's logged in. `User.IsInRole()` checks the user's claims.

**Lines 58-86 — Auth section (login/logout):**
```cshtml
@if (User.Identity?.IsAuthenticated ?? false)
{
    <li class="nav-item me-2">
        <a class="nav-link text-dark" asp-controller="Profile" asp-action="Index">
            <span class="me-1">👤</span>@User.Identity?.Name
        </a>
    </li>
    ...
    <li class="nav-item">
        <form asp-controller="Account" asp-action="Logout" method="post" class="d-inline">
            @Html.AntiForgeryToken()
            <button type="submit" class="btn btn-link nav-link">Logout</button>
        </form>
    </li>
}
else
{
    <li class="nav-item">
        <a class="nav-link text-dark" asp-controller="Account" asp-action="Login">Login</a>
    </li>
    ...
}
```
`User.Identity?.IsAuthenticated ?? false` — the `??` is the null-coalescing operator. If `Identity` is null, default to `false`.

**Line 94 — Content injection point:**
```cshtml
<main role="main" class="pb-3">
    @RenderBody()
</main>
```
This is where each view's content gets injected. Think of it as `{{{body}}}` in Handlebars.

**Line 106 — Per-page scripts:**
```cshtml
@await RenderSectionAsync("Scripts", required: false)
```
Views can inject scripts into this section. `required: false` means the section is optional.

---

## Razor Syntax Patterns — With Real Examples

### 1. Model Directive

Every view starts with a strongly-typed model:
```cshtml
@model SubjectViewModel
```
This tells the Razor engine: "This view receives a `SubjectViewModel` as its model." It enables `@Model.Name`, `asp-for="Name"`, etc.

**Collection model:**
```cshtml
@model IReadOnlyList<SubjectViewModel>
```
The Index view receives a list, not a single item.

### 2. Code Blocks

```cshtml
@{
    ViewData["Title"] = "Create Subject";
}
```
Executes C# code. Used for local variables and ViewData assignment. The `@{ }` block doesn't produce HTML — it runs C# silently.

### 3. Inline Expressions

```cshtml
<p>@Model.Name</p>
<p>@(Model.IsActive ? "Yes" : "No")</p>
<p>@Model.CreatedAt.ToString("MM/dd/yyyy")</p>
```
`@` outputs the result directly. Parentheses `@(...)` are needed for complex expressions.

**Null-safe navigation:**
```cshtml
<td>@(item.TeacherName ?? "Unassigned")</td>
```

**String interpolation:**
```cshtml
<p>@($"Enter Grades - {Model.SubjectCode}")</p>
```

### 4. Conditionals

**Basic if/else:**
```cshtml
@if (Model.Count == 0)
{
    <div class="alert alert-info">No subjects yet.</div>
}
else
{
    <div class="table-responsive">
        <table class="table table-striped">...</table>
    </div>
}
```

**Flash messages:**
```cshtml
@if (TempData["StatusMessage"] is string message)
{
    <div class="alert alert-success">@message</div>
}
```
`TempData` is a one-time-read dictionary — it's available for one request after being set in the controller, then it disappears. Perfect for success/error messages after a redirect.

### 5. Loops

**`@foreach` — iterate a collection:**
```cshtml
@foreach (var item in Model)
{
    <tr>
        <td><strong>@item.Code</strong></td>
        <td>@item.Name</td>
        <td><span class="badge bg-info">@item.Credits</span></td>
        <td>@item.TeacherName</td>
        <td class="text-end">
            <a asp-action="Edit" asp-route-id="@item.Id" class="btn btn-sm btn-outline-secondary">Edit</a>
        </td>
    </tr>
}
```

**`@for` — indexed loop (for form arrays):**
```cshtml
@for (int i = 0; i < Model.StudentGrades.Count; i++)
{
    var item = Model.StudentGrades[i];
    <tr>
        <td>
            <input type="hidden" name="StudentGrades[@i].StudentId" value="@item.StudentId" />
            <input type="hidden" name="StudentGrades[@i].Email" value="@item.Email" />
            <strong>@item.FullName</strong>
        </td>
        <td>
            <input type="number" name="StudentGrades[@i].Score"
                   value="@item.Score" min="0" max="100" step="0.5"
                   class="form-control" style="max-width: 120px;" />
        </td>
    </tr>
}
```
**Why `@for` instead of `@foreach`?** When you need to submit an array of items back to the controller, each input needs an indexed name like `StudentGrades[0].Score`, `StudentGrades[1].Score`, etc. The `@for` loop gives you the index `i` to build these names.

### 6. Tag Helpers — The .NET Superpower

Tag Helpers are server-side components that process HTML elements before rendering. They look like HTML attributes but generate real HTML.

**Form inputs — `asp-for`:**
```cshtml
<label asp-for="Code" class="form-label"></label>
<input asp-for="Code" class="form-control" placeholder="e.g. CS101" />
<span asp-validation-for="Code" class="text-danger"></span>
```
This generates:
```html
<label for="Code" class="form-label">Code</label>
<input id="Code" name="Code" class="form-control" placeholder="e.g. CS101" value="" />
<span class="text-danger" data-valmsg-for="Code" data-valmsg-replace="true"></span>
```
The Tag Helper reads the `[Required]`, `[StringLength]` attributes from the ViewModel and auto-generates `data-val-*` attributes for jQuery Validation.

**Links — `asp-action`, `asp-controller`:**
```cshtml
<a asp-action="Edit" asp-controller="Subjects" asp-route-id="@item.Id">Edit</a>
```
Generates: `<a href="/Subjects/Edit/3">Edit</a>`

**Dropdowns — `asp-items`:**
```cshtml
<select asp-for="TeacherId" asp-items="ViewBag.Teachers" class="form-select"></select>
```
`ViewBag.Teachers` is a `List<SelectListItem>` built by the controller. The Tag Helper renders each item as an `<option>`.

**Forms with anti-forgery:**
```cshtml
<form asp-action="Create" method="post">
    @Html.AntiForgeryToken()
    <!-- form fields -->
    <button type="submit" class="btn btn-primary">Create</button>
</form>
```
`@Html.AntiForgeryToken()` generates a hidden input with a CSRF token. The `[ValidateAntiForgeryToken]` attribute on the controller action validates it.

**Cache busting:**
```cshtml
<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
```
Appends `?v=hash` to prevent browser caching of old files.

### 7. Partial Views

```cshtml
<partial name="_ValidationScriptsPartial" />
```
Includes another view inline. `_ValidationScriptsPartial.cshtml` contains:
```html
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
```
This is how form views get client-side validation without loading scripts on every page.

### 8. Sections

```cshtml
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```
Injects content into `_Layout.cshtml`'s `@await RenderSectionAsync("Scripts", required: false)`. Only form pages need validation scripts, so they opt in via this section.

### 9. ViewData / ViewBag

```cshtml
<!-- In Controller: -->
ViewBag.Teachers = teacherList;  // List<SelectListItem>

<!-- In View: -->
<select asp-for="TeacherId" asp-items="ViewBag.Teachers" class="form-select"></select>
```

**ViewData vs ViewBag:**
- `ViewData["Title"]` — dictionary-based, requires casting
- `ViewBag.Teachers` — dynamic property, no casting needed (but no compile-time checking)

Both are just different syntax for the same thing.

---

## Real View Walkthrough: `Subjects/Create.cshtml` (42 lines)

```cshtml
@model SubjectViewModel                              ← line 1: declares model type
@{
    ViewData["Title"] = "Create Subject";             ← line 3: sets page title
}

<h1 class="mb-4">Create Subject</h1>                 ← line 6: heading

<form asp-action="Create" method="post">             ← line 8: POST to Create action
    @Html.AntiForgeryToken()                          ← line 9: CSRF token

    <div class="row">
        <div class="col-md-6 mb-3">
            <label asp-for="Code" class="form-label"></label>         ← line 13
            <input asp-for="Code" class="form-control" placeholder="e.g. CS101" />
            <span asp-validation-for="Code" class="text-danger"></span>
        </div>
        <div class="col-md-6 mb-3">
            <label asp-for="Credits" class="form-label"></label>
            <input asp-for="Credits" type="number" class="form-control" min="1" max="12" />
            <span asp-validation-for="Credits" class="text-danger"></span>
        </div>
    </div>

    <div class="mb-3">
        <label asp-for="Name" class="form-label"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>

    <div class="mb-3">
        <label asp-for="TeacherId" class="form-label">Teacher</label>
        <select asp-for="TeacherId" asp-items="ViewBag.Teachers" class="form-select"></select>
    </div>

    <button type="submit" class="btn btn-primary">Create</button>
    <a asp-action="Index" class="btn btn-link">Back to list</a>
</form>

@section Scripts {                                    ← line 40: inject validation scripts
    <partial name="_ValidationScriptsPartial" />
}
```

**What happens when the form is submitted:**
1. Browser POSTs form data to `/Subjects/Create`
2. Model binder maps form fields to `SubjectViewModel` properties
3. `ModelState.IsValid` checks `[Required]`, `[StringLength]`, `[Range]`
4. If valid → save to DB → `TempData["StatusMessage"] = "Subject created."` → redirect to `Index`
5. If invalid → return View with model → validation errors shown via `asp-validation-for`

---

## Real View Walkthrough: `Subjects/Index.cshtml` (55 lines)

```cshtml
@model IReadOnlyList<SubjectViewModel>               ← line 1: list model
@{
    ViewData["Title"] = "Subjects";
    var statusMessage = TempData["StatusMessage"] as string;  ← line 4: read flash message
}

<div class="d-flex align-items-center justify-content-between mb-3">
    <h1 class="mb-0">Subjects</h1>
    <a asp-action="Create" class="btn btn-primary">New subject</a>   ← line 9: create button
</div>

@if (!string.IsNullOrWhiteSpace(statusMessage))       ← line 12: show flash if exists
{
    <div class="alert alert-success">@statusMessage</div>
}

@if (Model.Count == 0)                                ← line 17: empty state
{
    <div class="alert alert-info">No subjects yet.</div>
}
else
{
    <div class="table-responsive">
        <table class="table table-striped align-middle">
            <thead>
                <tr>
                    <th>Code</th>
                    <th>Name</th>
                    <th>Credits</th>
                    <th>Teacher</th>
                    <th class="text-end">Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in Model)           ← line 35: loop through subjects
                {
                    <tr>
                        <td><strong>@item.Code</strong></td>
                        <td>@item.Name</td>
                        <td><span class="badge bg-info">@item.Credits</span></td>
                        <td>@item.TeacherName</td>
                        <td class="text-end">
                            <a asp-action="Edit" asp-route-id="@item.Id"
                               class="btn btn-sm btn-outline-secondary">Edit</a>
                            <a asp-controller="StudentSubjects"
                               asp-action="AssignStudents"
                               asp-route-subjectId="@item.Id"
                               class="btn btn-sm btn-outline-info">Assign</a>
                            <form asp-action="Delete" asp-route-id="@item.Id"
                                  method="post" class="d-inline">
                                @Html.AntiForgeryToken()
                                <button type="submit" class="btn btn-sm btn-outline-danger"
                                        onclick="return confirm('Are you sure?')">Delete</button>
                            </form>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}
```

**Key patterns visible here:**
- `TempData["StatusMessage"]` — flash message after create/edit/delete
- `Model.Count == 0` — empty state handling
- `@foreach (var item in Model)` — loop through list
- `asp-route-id="@item.Id"` — pass ID in URL
- `onclick="return confirm('Are you sure?')"` — JavaScript confirmation before delete
- `class="d-inline"` — makes the form inline with the buttons next to it

---

## Real View Walkthrough: `Grades/Enter.cshtml` — Form Array Pattern (68 lines)

The grade entry form is the most complex view because it submits an **array of items**:

```cshtml
@model GradeEntryViewModel
@{
    ViewData["Title"] = $"Enter Grades - {Model.SubjectCode}";
}

<h1 class="mb-3">Enter Grades</h1>
<p class="text-muted">@Model.SubjectCode — @Model.SubjectName</p>

@if (TempData["StatusMessage"] is string message)
{
    <div class="alert alert-success">@message</div>
}

<form asp-action="Enter" asp-route-subjectId="@Model.SubjectId" method="post">
    @Html.AntiForgeryToken()

    <div class="table-responsive">
        <table class="table table-striped align-middle">
            <thead>
                <tr>
                    <th>Student</th>
                    <th>Email</th>
                    <th>Score (0-100)</th>
                    <th>Letter Grade</th>
                </tr>
            </thead>
            <tbody>
                @for (int i = 0; i < Model.StudentGrades.Count; i++)
                {
                    var item = Model.StudentGrades[i];
                    <tr>
                        <td>
                            <input type="hidden" name="StudentGrades[@i].StudentId" value="@item.StudentId" />
                            <input type="hidden" name="StudentGrades[@i].Email" value="@item.Email" />
                            <input type="hidden" name="StudentGrades[@i].FullName" value="@item.FullName" />
                            <input type="hidden" name="StudentGrades[@i].StudentNumber" value="@item.StudentNumber" />
                            <input type="hidden" name="StudentGrades[@i].Semester" value="@item.Semester" />
                            <input type="hidden" name="StudentGrades[@i].Year" value="@item.Year" />
                            <strong>@item.FullName</strong>
                        </td>
                        <td>@item.Email</td>
                        <td>
                            <input type="number" name="StudentGrades[@i].Score"
                                   value="@item.Score" min="0" max="100" step="0.5"
                                   class="form-control" style="max-width: 120px;" />
                        </td>
                        <td>
                            <span class="badge bg-secondary">
                                @(string.IsNullOrWhiteSpace(item.LetterGrade) ? "-" : item.LetterGrade)
                            </span>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>

    <button type="submit" class="btn btn-primary">Save Grades</button>
</form>
```

**Why hidden inputs?** The form only has one editable field (`Score`), but the controller needs all fields to create/update the `Grade` record. Hidden inputs carry the non-editable data back.

**Why `name="StudentGrades[@i].Score"`?** This is the ASP.NET model binding convention. When the form is submitted, the model binder sees:
```
StudentGrades[0].Score = 92
StudentGrades[1].Score = 75
StudentGrades[2].Score = 88
```
And deserializes it into `GradeEntryViewModel.StudentGrades` as a `List<StudentGradeItem>`.

---

## Request → View Flow

```
1. Controller action:
   return View("Index", viewModel);

2. Razor engine:
   a. Finds Views/Subjects/Index.cshtml (by convention)
   b. Reads _ViewStart.cshtml → applies _Layout.cshtml
   c. Processes @model directive → binds viewModel
   d. Executes Razor syntax (C# + HTML)
   e. Injects into _Layout's @RenderBody()
   f. Returns complete HTML to browser
```

---

## Key Differences from NestJS Templates

| Feature | Razor (.cshtml) | NestJS Handlebars/EJS |
|---|---|---|
| Language | Full C# | Limited JS expressions |
| Type safety | `@model` strongly typed | Usually untyped |
| Tag Helpers | Auto-generate HTML attributes | Manual HTML |
| Layout | `_Layout.cshtml` with `@RenderBody()` | `{{> partial}}` or `{{{body}}}` |
| Validation | Tag helpers auto-add `data-val-*` | Manual |
| Partials | `<partial name="..." />` | `{{> partial}}` |
| Sections | `@section Scripts { }` | Limited |
| Model binding | Auto-binds form → ViewModel | Manual `req.body` parsing |
| Flash messages | `TempData` (one-read) | Session flash |
