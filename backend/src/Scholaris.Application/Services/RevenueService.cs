using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Scholaris.Application.Common;
using Scholaris.Application.Dtos;
using Scholaris.Domain.Entities;
using Scholaris.Domain.Enums;

namespace Scholaris.Application.Services;

/// <summary>
/// Revenue reporting built on the commission ledger (Orders with Status=Paid):
/// Gross = what students paid (after admin-set discount), Commission = platform service
/// income at the snapshotted rate, Net = teacher income. Grouped by teacher and month,
/// exportable as CSV.
/// </summary>
public class RevenueService(IAppDbContext db)
{
    public Task<List<RevenueRowDto>> AdminRevenueAsync(int year) => QueryAsync(null, year);

    public Task<List<RevenueRowDto>> TeacherRevenueAsync(Guid teacherId, int year) => QueryAsync(teacherId, year);

    private async Task<List<RevenueRowDto>> QueryAsync(Guid? teacherId, int year)
    {
        var q = db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Paid && o.PaidAt != null && o.PaidAt!.Value.Year == year);

        if (teacherId != null)
            q = q.Where(o => o.Course!.TeacherId == teacherId);

        var flat = await q
            .Select(o => new
            {
                Teacher = o.Course!.Teacher.FullName,
                Month = o.PaidAt!.Value.Month,
                o.FinalAmount,
                o.CommissionAmount
            })
            .ToListAsync();

        return flat
            .GroupBy(x => new { x.Teacher, x.Month })
            .Select(g => new RevenueRowDto(
                g.Key.Teacher,
                $"{year}-{g.Key.Month:D2}",
                g.Sum(x => x.FinalAmount),
                g.Sum(x => x.CommissionAmount),
                g.Sum(x => x.FinalAmount - x.CommissionAmount),
                g.Count()))
            .OrderBy(r => r.TeacherName)
            .ThenBy(r => r.Month)
            .ToList();
    }

    public static string ToCsv(IEnumerable<RevenueRowDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Teacher,Month,Gross ETB,Service Commission ETB,Net Teacher Income ETB,Orders");
        foreach (var r in rows)
            sb.AppendLine(string.Join(",",
                Esc(r.TeacherName), r.Month,
                r.Gross.ToString("0.00", CultureInfo.InvariantCulture),
                r.Commission.ToString("0.00", CultureInfo.InvariantCulture),
                r.Net.ToString("0.00", CultureInfo.InvariantCulture),
                r.Orders));
        return sb.ToString();
    }

    private static string Esc(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
}
