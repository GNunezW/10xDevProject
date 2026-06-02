using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public interface IInstructionTypeService
{
    Task<List<InstructionType>> GetAllAsync();
    Task<InstructionType?> GetByIdAsync(int id);
    Task<InstructionType> CreateAsync(InstructionType type);
    Task UpdateAsync(InstructionType type);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}
