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
    public DbSet<LecturerAvailability> LecturerAvailabilities => Set<LecturerAvailability>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<InstructionType> InstructionTypes => Set<InstructionType>();
    public DbSet<StudyProgramEnrollment> StudyProgramEnrollments => Set<StudyProgramEnrollment>();

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

        modelBuilder.Entity<InstructionType>()
            .HasIndex(t => t.Name)
            .IsUnique();

        modelBuilder.Entity<InstructionType>()
            .HasData(
                new InstructionType { Id = 1, Name = "Wykład", MaxStudentsPerGroup = 120 },
                new InstructionType { Id = 2, Name = "Ćwiczenia", MaxStudentsPerGroup = 30 },
                new InstructionType { Id = 3, Name = "Laboratorium", MaxStudentsPerGroup = 24 });

        modelBuilder.Entity<LecturerAvailability>()
            .HasKey(la => new { la.LecturerId, la.DayOfWeek, la.TimeSlotId });

        modelBuilder.Entity<LecturerAvailability>()
            .HasOne(la => la.Lecturer)
            .WithMany()
            .HasForeignKey(la => la.LecturerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LecturerAvailability>()
            .HasOne(la => la.TimeSlot)
            .WithMany()
            .HasForeignKey(la => la.TimeSlotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudyProgramEnrollment>()
            .HasKey(e => new { e.StudyProgramId, e.Semester });

        modelBuilder.Entity<StudyProgramEnrollment>()
            .HasOne(e => e.StudyProgram)
            .WithMany()
            .HasForeignKey(e => e.StudyProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Room>()
            .HasOne(r => r.InstructionType)
            .WithMany()
            .HasForeignKey(r => r.InstructionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subject>()
            .HasOne(s => s.InstructionType)
            .WithMany()
            .HasForeignKey(s => s.InstructionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
