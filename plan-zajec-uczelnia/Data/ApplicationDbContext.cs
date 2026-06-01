using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<StudyProgram> StudyPrograms => Set<StudyProgram>();
    public DbSet<Lecturer> Lecturers => Set<Lecturer>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SubjectLecturer> SubjectLecturers => Set<SubjectLecturer>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<SemesterPeriod> SemesterPeriods => Set<SemesterPeriod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SubjectLecturer>()
            .HasKey(sl => new { sl.SubjectId, sl.LecturerId });

        modelBuilder.Entity<StudyProgram>()
            .HasMany(sp => sp.Subjects)
            .WithOne(s => s.StudyProgram)
            .HasForeignKey(s => s.StudyProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Lecturer>()
            .HasIndex(l => l.Email)
            .IsUnique();

        modelBuilder.Entity<SemesterPeriod>()
            .HasIndex(sp => new { sp.AcademicYear, sp.SemesterOrdinal })
            .IsUnique();
    }
}
