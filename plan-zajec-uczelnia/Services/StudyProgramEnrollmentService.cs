using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class StudyProgramEnrollmentService(ApplicationDbContext db) : IStudyProgramEnrollmentService
{
    public async Task<Dictionary<int, int>> GetByProgramAsync(int studyProgramId)
    {
        var rows = await db.StudyProgramEnrollments
            .AsNoTracking()
            .Where(e => e.StudyProgramId == studyProgramId)
            .ToListAsync();

        return rows.ToDictionary(e => e.Semester, e => e.StudentCount);
    }

    public async Task<int> GetStudentCountAsync(int studyProgramId, int semester)
    {
        var entry = await db.StudyProgramEnrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudyProgramId == studyProgramId && e.Semester == semester);

        return entry?.StudentCount ?? 0;
    }

    public async Task SaveAsync(int studyProgramId, IEnumerable<(int Semester, int StudentCount)> entries)
    {
        var existing = db.StudyProgramEnrollments.Where(e => e.StudyProgramId == studyProgramId);
        db.StudyProgramEnrollments.RemoveRange(existing);

        foreach (var (semester, count) in entries.Where(e => e.StudentCount > 0))
        {
            db.StudyProgramEnrollments.Add(new StudyProgramEnrollment
            {
                StudyProgramId = studyProgramId,
                Semester = semester,
                StudentCount = count
            });
        }

        await db.SaveChangesAsync();
    }
}
