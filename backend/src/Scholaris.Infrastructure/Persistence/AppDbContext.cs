using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Domain.Entities;

namespace Scholaris.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LessonCompletion> LessonCompletions => Set<LessonCompletion>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<CertificateTemplate> CertificateTemplates => Set<CertificateTemplate>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<QaThread> QaThreads => Set<QaThread>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FullName).HasMaxLength(120);
            e.Property(u => u.Email).HasMaxLength(160);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasIndex(t => t.Token).IsUnique();
            e.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Course>(e =>
        {
            e.HasIndex(c => c.Slug).IsUnique();
            e.Property(c => c.Title).HasMaxLength(160);
            e.Property(c => c.Price).HasPrecision(18, 2);
            e.Property(c => c.DiscountPercent).HasPrecision(5, 2);
            e.HasOne(c => c.Teacher).WithMany().HasForeignKey(c => c.TeacherId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Module>().HasOne(m => m.Course).WithMany(c => c.Modules).HasForeignKey(m => m.CourseId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Topic>().HasOne(t => t.Module).WithMany(m => m.Topics).HasForeignKey(t => t.ModuleId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Lesson>().HasOne(l => l.Topic).WithMany(t => t.Lessons).HasForeignKey(l => l.TopicId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Enrollment>(e =>
        {
            e.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<LessonCompletion>(e =>
        {
            e.HasIndex(x => new { x.StudentId, x.LessonId }).IsUnique();
            e.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Order>(e =>
        {
            e.Property(o => o.GrossAmount).HasPrecision(18, 2);
            e.Property(o => o.DiscountAmount).HasPrecision(18, 2);
            e.Property(o => o.FinalAmount).HasPrecision(18, 2);
            e.Property(o => o.CommissionRatePercent).HasPrecision(5, 2);
            e.Property(o => o.CommissionAmount).HasPrecision(18, 2);
            e.Property(o => o.NetAmount).HasPrecision(18, 2);
            // Orders are the permanent revenue ledger — never cascade-deleted with a course.
            e.HasOne(o => o.Course).WithMany().HasForeignKey(o => o.CourseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Student).WithMany().HasForeignKey(o => o.StudentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PlatformSetting>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.CommissionRatePercent).HasPrecision(5, 2);
        });

        b.Entity<CertificateTemplate>(e => { e.HasKey(t => t.Id); });

        b.Entity<Certificate>(e =>
        {
            e.HasIndex(c => c.SerialCode).IsUnique();
            e.HasIndex(c => new { c.StudentId, c.CourseId }).IsUnique();
            e.HasOne(c => c.Student).WithMany().HasForeignKey(c => c.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Course).WithMany().HasForeignKey(c => c.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<QaThread>(e =>
        {
            e.HasOne(t => t.Course).WithMany().HasForeignKey(t => t.CourseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Student).WithMany().HasForeignKey(t => t.StudentId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
