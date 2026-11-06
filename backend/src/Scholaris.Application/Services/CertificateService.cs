using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

/// <summary>
/// Certificates are issued automatically on course completion (see LearnService) and rendered
/// from the admin-maintained CertificateTemplate. The view is publicly verifiable by serial.
/// </summary>
public class CertificateService(IAppDbContext db, ICurrentUser currentUser, SettingsService settings)
{
    public async Task<List<CertificateViewDto>> MineAsync()
    {
        var uid = currentUser.UserId ?? throw new ForbiddenException("Please sign in to continue.");
        var certs = await db.Certificates.AsNoTracking()
            .Include(c => c.Course)
            .Where(c => c.StudentId == uid)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();
        var template = await settings.GetTemplateDtoAsync();
        return certs.Select(c => ToView(c, c.Course.Title, template)).ToList();
    }

    /// <summary>Public verification endpoint payload — no auth required.</summary>
    public async Task<CertificateViewDto> GetBySerialAsync(string serial)
    {
        var cert = await db.Certificates.AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.Student)
            .FirstOrDefaultAsync(c => c.SerialCode == serial)
            ?? throw new NotFoundException("No certificate found for this serial.");

        var template = await settings.GetTemplateDtoAsync();
        return ToView(cert, cert.Course.Title, template, cert.Student.FullName);
    }

    private static CertificateViewDto ToView(Certificate c, string courseTitle, CertificateTemplateDto template, string? studentName = null) =>
        new(c.SerialCode, studentName ?? "", courseTitle, c.IssuedAt, template);
}
