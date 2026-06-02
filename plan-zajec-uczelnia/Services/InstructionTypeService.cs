using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Data;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Services;

public class InstructionTypeService(ApplicationDbContext db) : IInstructionTypeService
{
    public Task<List<InstructionType>> GetAllAsync() =>
        db.InstructionTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync();

    public Task<InstructionType?> GetByIdAsync(int id) =>
        db.InstructionTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

    public async Task<InstructionType> CreateAsync(InstructionType type)
    {
        db.InstructionTypes.Add(type);
        await db.SaveChangesAsync();
        return type;
    }

    public async Task UpdateAsync(InstructionType type)
    {
        var existing = await db.InstructionTypes.FindAsync(type.Id);
        if (existing is null) return;
        existing.Name = type.Name;
        existing.MaxStudentsPerGroup = type.MaxStudentsPerGroup;
        await db.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var usedByRoom = await db.Rooms.AnyAsync(r => r.InstructionTypeId == id);
        var usedBySubject = await db.Subjects.AnyAsync(s => s.InstructionTypeId == id);
        if (usedByRoom || usedBySubject)
            return (false, "Typ jest używany przez sale lub przedmioty — najpierw zmień przypisania.");

        var entity = await db.InstructionTypes.FindAsync(id);
        if (entity is null) return (true, null);

        db.InstructionTypes.Remove(entity);
        await db.SaveChangesAsync();
        return (true, null);
    }
}
