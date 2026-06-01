using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface IStudyProgramService
{
    Task<List<StudyProgram>> GetAllAsync();
    Task<StudyProgram?> GetByIdAsync(int id);
    Task<StudyProgram> CreateAsync(StudyProgram program);
    Task UpdateAsync(StudyProgram program);
    Task DeleteAsync(int id);
    Task<bool> HasSubjectsAsync(int programId);
}
