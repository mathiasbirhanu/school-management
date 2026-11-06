using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Exceptions;

namespace Scholaris.Application.Services;

/// <summary>
/// Admin-adjustable platform configuration: the service commission ("income") rate that is
/// snapshotted into every order, and the certificate template whose wording/design the admin
/// controls from the back office.
/// </summary>
public class SettingsService(IAppDbContext db)
{
    public async Task<PlatformSetting> GetSettingAsync()
    {
        var setting = await db.PlatformSettings.FirstOrDefaultAsync(s => s.Id == 1);
        if (setting == null)
        {
            setting = new PlatformSetting();
            db.PlatformSettings.Add(setting);
            await db.SaveChangesAsync();
        }
        return setting;
    }

    public async Task<PlatformSettingsDto> GetAsync()
    {
        var s = await GetSettingAsync();
        return new PlatformSettingsDto(s.CommissionRatePercent, s.SiteName, s.UpdatedAt);
    }

    public async Task<PlatformSettingsDto> UpdateAsync(UpdateSettingsRequest req)
    {
        if (req.CommissionRatePercent is < 0 or > 90)
            throw new DomainException("Commission rate must be between 0 and 90 percent.");
        var s = await GetSettingAsync();
        s.CommissionRatePercent = decimal.Round(req.CommissionRatePercent, 2);
        s.SiteName = string.IsNullOrWhiteSpace(req.SiteName) ? s.SiteName : req.SiteName.Trim();
        s.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return new PlatformSettingsDto(s.CommissionRatePercent, s.SiteName, s.UpdatedAt);
    }

    public async Task<CertificateTemplate> GetTemplateAsync()
    {
        var t = await db.CertificateTemplates.FirstOrDefaultAsync(t => t.Id == 1);
        if (t == null)
        {
            t = new CertificateTemplate();
            db.CertificateTemplates.Add(t);
            await db.SaveChangesAsync();
        }
        return t;
    }

    public async Task<CertificateTemplateDto> GetTemplateDtoAsync()
    {
        var t = await GetTemplateAsync();
        return ToDto(t);
    }

    public async Task<CertificateTemplateDto> UpdateTemplateAsync(CertificateTemplateDto req)
    {
        if (string.IsNullOrWhiteSpace(req.BodyTemplate) || !req.BodyTemplate.Contains("{{StudentName}}"))
            throw new DomainException("Certificate body must contain the {{StudentName}} placeholder.");
        var t = await GetTemplateAsync();
        t.Title = req.Title.Trim();
        t.Subtitle = req.Subtitle.Trim();
        t.BodyTemplate = req.BodyTemplate;
        t.FooterNote = req.FooterNote;
        t.SignatoryName = req.SignatoryName.Trim();
        t.SignatoryTitle = req.SignatoryTitle.Trim();
        t.AccentHex = string.IsNullOrWhiteSpace(req.AccentHex) ? t.AccentHex : req.AccentHex.Trim();
        t.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return ToDto(t);
    }

    public static CertificateTemplateDto ToDto(CertificateTemplate t) =>
        new(t.Title, t.Subtitle, t.BodyTemplate, t.FooterNote, t.SignatoryName, t.SignatoryTitle, t.AccentHex);
}
