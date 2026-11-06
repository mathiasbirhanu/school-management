using Microsoft.EntityFrameworkCore;
using Scholaris.Domain.Entities;

namespace Scholaris.Application.Common;

/// <summary>Persistence abstraction consumed by the Application layer (implemented with EF Core in Infrastructure).</summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Course> Courses { get; }
    DbSet<Module> Modules { get; }
    DbSet<Topic> Topics { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<LessonCompletion> LessonCompletions { get; }
    DbSet<Order> Orders { get; }
    DbSet<PlatformSetting> PlatformSettings { get; }
    DbSet<CertificateTemplate> CertificateTemplates { get; }
    DbSet<Certificate> Certificates { get; }
    DbSet<QaThread> QaThreads { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
