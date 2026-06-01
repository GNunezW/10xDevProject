using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class LecturerService(ApplicationDbContext db) : ILecturerService
{
    public Task<List<Lecturer>> GetAllAsync() =>
        db.Lecturers.AsNoTracking().OrderBy(l => l.LastName).ThenBy(l => l.FirstName).ToListAsync();

    public Task<Lecturer?> GetByIdAsync(int id) =>
        db.Lecturers.FindAsync(id).AsTask();

    public async Task<Lecturer> CreateAsync(Lecturer lecturer)
    {
        db.Lecturers.Add(lecturer);
        await db.SaveChangesAsync();
        return lecturer;
    }

    public async Task UpdateAsync(Lecturer lecturer)
    {
        var existing = await db.Lecturers.FindAsync(lecturer.Id);
        if (existing is null) return;
        existing.FirstName = lecturer.FirstName;
        existing.LastName = lecturer.LastName;
        existing.Email = lecturer.Email;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var lecturer = await db.Lecturers.FindAsync(id);
        if (lecturer is not null)
        {
            db.Lecturers.Remove(lecturer);
            await db.SaveChangesAsync();
        }
    }
}
