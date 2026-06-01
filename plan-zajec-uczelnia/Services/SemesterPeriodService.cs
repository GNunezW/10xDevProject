using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class SemesterPeriodService(ApplicationDbContext db) : ISemesterPeriodService
{
    public Task<List<SemesterPeriod>> GetAllAsync() =>
        db.SemesterPeriods.AsNoTracking()
            .OrderBy(p => p.AcademicYear)
            .ThenBy(p => p.SemesterOrdinal)
            .ToListAsync();

    public async Task<SemesterPeriod> CreateAsync(SemesterPeriod period)
    {
        var duplicate = await db.SemesterPeriods.AnyAsync(p =>
            p.AcademicYear == period.AcademicYear && p.SemesterOrdinal == period.SemesterOrdinal);
        if (duplicate)
            throw new InvalidOperationException(
                $"Semestr {(period.SemesterOrdinal == 1 ? "zimowy" : "letni")} {period.AcademicYear} już istnieje.");

        db.SemesterPeriods.Add(period);
        await db.SaveChangesAsync();
        return period;
    }

    public async Task UpdateAsync(SemesterPeriod period)
    {
        var existing = await db.SemesterPeriods.FindAsync(period.Id);
        if (existing is null) return;
        existing.AcademicYear = period.AcademicYear;
        existing.SemesterOrdinal = period.SemesterOrdinal;
        existing.StartDate = period.StartDate;
        existing.EndDate = period.EndDate;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var period = await db.SemesterPeriods.FindAsync(id);
        if (period is not null)
        {
            db.SemesterPeriods.Remove(period);
            await db.SaveChangesAsync();
        }
    }
}
