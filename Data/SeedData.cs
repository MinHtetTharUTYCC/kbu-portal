using KbuPortal.Models;
using Microsoft.AspNetCore.Identity;

namespace KbuPortal.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create roles
        string[] roles = ["Admin", "Teacher", "Student"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Admin
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

        // Seed Teachers
        var teacherEmails = new[] { "john.smith@kbu.local", "jane.doe@kbu.local" };
        var teachers = new List<ApplicationUser>();

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

        // Seed Students
        var studentEmails = new[]
        {
            ("alice@kbu.local", "Alice", "Johnson", "STU001", "Engineering", "Computer Science"),
            ("bob@kbu.local", "Bob", "Williams", "STU002", "Engineering", "Electrical"),
            ("charlie@kbu.local", "Charlie", "Brown", "STU003", "Business", "Management"),
            ("diana@kbu.local", "Diana", "Lee", "STU004", "Arts", "Design"),
            ("eve@kbu.local", "Eve", "Martinez", "STU005", "Business", "Accounting")
        };
        var students = new List<ApplicationUser>();

        foreach (var (email, first, last, stuId, faculty, major) in studentEmails)
        {
            var student = await userManager.FindByEmailAsync(email);
            if (student == null)
            {
                student = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = first,
                    LastName = last,
                    StudentId = stuId,
                    Faculty = faculty,
                    Major = major
                };
                var result = await userManager.CreateAsync(student, "Student123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(student, "Student");
                }
            }
            students.Add(student);
        }

        // Seed Subjects
        if (!db.Subjects.Any())
        {
            var subjects = new[]
            {
                new Subject
                {
                    Code = "CS101",
                    Name = "Introduction to Programming",
                    Credits = 3,
                    TeacherId = teachers[0].Id
                },
                new Subject
                {
                    Code = "CS201",
                    Name = "Data Structures",
                    Credits = 4,
                    TeacherId = teachers[1].Id
                },
                new Subject
                {
                    Code = "MATH101",
                    Name = "Calculus I",
                    Credits = 4,
                    TeacherId = teachers[0].Id
                }
            };

            foreach (var subject in subjects)
            {
                db.Subjects.Add(subject);
            }
            await db.SaveChangesAsync();
        }

        // Seed Student-Subject Assignments
        if (!db.StudentSubjects.Any())
        {
            var subjects = db.Subjects.ToList();
            foreach (var student in students)
            {
                foreach (var subject in subjects.Take(2))
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

        // Seed Grades
        if (!db.Grades.Any())
        {
            var currentYear = DateTime.Now.Year;
            var grades = new[]
            {
                (studentId: students[0].Id, subjectId: 1, score: 92m, semester: 1),
                (studentId: students[0].Id, subjectId: 2, score: 88m, semester: 1),
                (studentId: students[1].Id, subjectId: 1, score: 75m, semester: 1),
                (studentId: students[1].Id, subjectId: 2, score: 82m, semester: 1),
                (studentId: students[2].Id, subjectId: 1, score: 95m, semester: 1),
                (studentId: students[2].Id, subjectId: 3, score: 89m, semester: 1),
            };

            foreach (var (studentId, subjectId, score, semester) in grades)
            {
                var letterGrade = score >= 90 ? "A"
                    : score >= 80 ? "B"
                    : score >= 70 ? "C"
                    : score >= 60 ? "D"
                    : "F";

                db.Grades.Add(new Grade
                {
                    StudentId = studentId,
                    SubjectId = subjectId,
                    Score = score,
                    LetterGrade = letterGrade,
                    Semester = semester,
                    Year = currentYear
                });
            }
            await db.SaveChangesAsync();
        }

        // Seed Schedule
        if (!db.Schedules.Any())
        {
            var subjects = db.Subjects.ToList();
            var schedules = new[]
            {
                new Schedule
                {
                    SubjectId = subjects[0].Id,
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Room = "A101"
                },
                new Schedule
                {
                    SubjectId = subjects[0].Id,
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    Room = "A101"
                },
                new Schedule
                {
                    SubjectId = subjects[1].Id,
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeSpan(11, 0, 0),
                    EndTime = new TimeSpan(12, 30, 0),
                    Room = "B205"
                },
                new Schedule
                {
                    SubjectId = subjects[1].Id,
                    DayOfWeek = DayOfWeek.Thursday,
                    StartTime = new TimeSpan(11, 0, 0),
                    EndTime = new TimeSpan(12, 30, 0),
                    Room = "B205"
                },
                new Schedule
                {
                    SubjectId = subjects[2].Id,
                    DayOfWeek = DayOfWeek.Friday,
                    StartTime = new TimeSpan(14, 0, 0),
                    EndTime = new TimeSpan(15, 30, 0),
                    Room = "C301"
                }
            };

            foreach (var schedule in schedules)
            {
                db.Schedules.Add(schedule);
            }
            await db.SaveChangesAsync();
        }

        // Seed Announcements
        if (!db.Announcements.Any())
        {
            var announcements = new[]
            {
                new Announcement
                {
                    Title = "Welcome to KBU Portal",
                    Content = "Welcome to the Kasem Bundit University student portal. Use this platform to manage your courses, view grades, and stay updated with announcements.",
                    CreatedAt = DateTime.UtcNow,
                    CreatedById = adminUser.Id,
                    IsPinned = true,
                    TargetRole = "All"
                },
                new Announcement
                {
                    Title = "Semester 1 Grades Now Available",
                    Content = "All semester 1 grades have been entered and are now visible in your grade report. Contact your instructor if you have questions.",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    CreatedById = adminUser.Id,
                    IsPinned = false,
                    TargetRole = "Student"
                },
                new Announcement
                {
                    Title = "Teacher Training Session",
                    Content = "All teachers are required to attend the training session on grade entry procedures this Friday.",
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    CreatedById = adminUser.Id,
                    IsPinned = true,
                    TargetRole = "Teacher"
                }
            };

            foreach (var announcement in announcements)
            {
                db.Announcements.Add(announcement);
            }
            await db.SaveChangesAsync();
        }
    }
}

