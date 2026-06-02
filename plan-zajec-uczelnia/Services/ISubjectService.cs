using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface ISubjectService
{
    Task<List<Subject>> GetAllAsync(int? studyProgramId = null);
    Task<Subject?> GetByIdWithLecturersAsync(int id);
    Task<Subject> CreateAsync(Subject subject, IEnumerable<int> lecturerIds);
    Task UpdateAsync(Subject subject, IEnumerable<int> lecturerIds);
    Task DeleteAsync(int id);
}
