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
