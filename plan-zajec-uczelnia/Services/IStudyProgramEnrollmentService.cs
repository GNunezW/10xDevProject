namespace plan_zajec_uczelnia.Services;

public interface IStudyProgramEnrollmentService
{
    Task<Dictionary<int, int>> GetByProgramAsync(int studyProgramId);
    Task SaveAsync(int studyProgramId, IEnumerable<(int Semester, int StudentCount)> entries);
    Task<int> GetStudentCountAsync(int studyProgramId, int semester);
}
