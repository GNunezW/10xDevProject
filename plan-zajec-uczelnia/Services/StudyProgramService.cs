using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class StudyProgramService(ApplicationDbContext db) : IStudyProgramService
{
    public Task<List<StudyProgram>> GetAllAsync() =>
        db.StudyPrograms.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

    public Task<StudyProgram?> GetByIdAsync(int id) =>
        db.StudyPrograms.FindAsync(id).AsTask();

    public async Task<StudyProgram> CreateAsync(StudyProgram program)
    {
        db.StudyPrograms.Add(program);
        await db.SaveChangesAsync();
        return program;
    }

    public async Task UpdateAsync(StudyProgram program)
    {
        var existing = await db.StudyPrograms.FindAsync(program.Id);
        if (existing is null) return;
        existing.Name = program.Name;
        existing.StudyMode = program.StudyMode;
        existing.StudyDegree = program.StudyDegree;
        existing.AcademicYear = program.AcademicYear;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var program = await db.StudyPrograms.FindAsync(id);
        if (program is not null)
        {
            db.StudyPrograms.Remove(program);
            await db.SaveChangesAsync();
        }
    }

    public Task<bool> HasSubjectsAsync(int programId) =>
        db.Subjects.AnyAsync(s => s.StudyProgramId == programId);
}
