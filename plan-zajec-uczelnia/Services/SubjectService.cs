using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class SubjectService(ApplicationDbContext db) : ISubjectService
{
    public Task<List<Subject>> GetAllAsync(int? studyProgramId = null)
    {
        var query = db.Subjects
            .AsNoTracking()
            .Include(s => s.StudyProgram)
            .Include(s => s.InstructionType)
            .Include(s => s.SubjectLecturers)
                .ThenInclude(sl => sl.Lecturer)
            .AsQueryable();

        if (studyProgramId.HasValue)
            query = query.Where(s => s.StudyProgramId == studyProgramId.Value);

        return query.OrderBy(s => s.Semester).ThenBy(s => s.Name).ToListAsync();
    }

    public Task<Subject?> GetByIdWithLecturersAsync(int id) =>
        db.Subjects
            .AsNoTracking()
            .Include(s => s.InstructionType)
            .Include(s => s.SubjectLecturers)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Subject> CreateAsync(Subject subject, IEnumerable<int> lecturerIds)
    {
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();

        foreach (var lecturerId in lecturerIds)
            db.SubjectLecturers.Add(new SubjectLecturer { SubjectId = subject.Id, LecturerId = lecturerId });

        await db.SaveChangesAsync();
        return subject;
    }

    public async Task UpdateAsync(Subject subject, IEnumerable<int> lecturerIds)
    {
        var existing = await db.Subjects.FindAsync(subject.Id);
        if (existing is null) return;
        existing.Name = subject.Name;
        existing.StudyProgramId = subject.StudyProgramId;
        existing.Semester = subject.Semester;
        existing.NumberOfSessions = subject.NumberOfSessions;
        existing.InstructionTypeId = subject.InstructionTypeId;

        var existingLinks = await db.SubjectLecturers
            .Where(sl => sl.SubjectId == subject.Id)
            .ToListAsync();
        db.SubjectLecturers.RemoveRange(existingLinks);

        foreach (var lecturerId in lecturerIds)
            db.SubjectLecturers.Add(new SubjectLecturer { SubjectId = subject.Id, LecturerId = lecturerId });

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var subject = await db.Subjects.FindAsync(id);
        if (subject is not null)
        {
            db.Subjects.Remove(subject);
            await db.SaveChangesAsync();
        }
    }
}
