using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class NonWorkingDayService(ApplicationDbContext db) : INonWorkingDayService
{
    public Task<List<NonWorkingDay>> GetAllAsync() =>
        db.NonWorkingDays.AsNoTracking()
            .OrderBy(d => d.Date)
            .ThenBy(d => d.Name)
            .ToListAsync();

    public async Task<NonWorkingDay> CreateAsync(NonWorkingDay day)
    {
        await ValidateUniqueAsync(day);
        db.NonWorkingDays.Add(day);
        await db.SaveChangesAsync();
        return day;
    }

    public async Task UpdateAsync(NonWorkingDay day)
    {
        var existing = await db.NonWorkingDays.FindAsync(day.Id);
        if (existing is null) return;

        await ValidateUniqueAsync(day, day.Id);
        existing.AcademicYear = string.IsNullOrWhiteSpace(day.AcademicYear) ? null : day.AcademicYear.Trim();
        existing.Date = day.Date;
        existing.Name = day.Name;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.NonWorkingDays.FindAsync(id);
        if (entity is not null)
        {
            db.NonWorkingDays.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    private async Task ValidateUniqueAsync(NonWorkingDay day, int? excludeId = null)
    {
        var year = string.IsNullOrWhiteSpace(day.AcademicYear) ? null : day.AcademicYear.Trim();
        var duplicate = await db.NonWorkingDays.AnyAsync(d =>
            d.Id != excludeId
            && d.Date == day.Date
            && d.AcademicYear == year);
        if (duplicate)
            throw new InvalidOperationException("Ten dzień wolny już istnieje dla wybranego roku akademickiego.");
    }
}
