using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Data.Seeding;

internal static class DemoSeedInfrastructure
{
    internal record LecturerDef(string FirstName, string LastName, string Email);

    internal record SubjectDef(
        int Semester,
        string Name,
        int InstructionTypeId,
        int Sessions,
        string PrimaryEmail,
        string? SecondaryEmail = null);

    internal static readonly TimeOnly[] StandardSlotStarts =
    [
        new(8, 0),
        new(9, 45),
        new(11, 30),
        new(13, 15),
        new(15, 0),
        new(16, 45),
        new(18, 30)
    ];

    internal static readonly DayOfWeek[] Weekdays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    /// <summary>Godziny akademickie (45 min) → bloki 90 min w semestrze (~15 tygodni).</summary>
    internal static int SessionsFromHours(int academicHours) =>
        Math.Max(1, academicHours / 2);

    internal static async Task EnsureTimeSlotsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var existing = await db.TimeSlots.Select(s => s.StartTime).ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        foreach (var start in StandardSlotStarts)
        {
            if (existingSet.Contains(start))
                continue;

            db.TimeSlots.Add(new TimeSlot { StartTime = start });
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    internal static async Task EnsureRoomsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var existing = await db.Rooms.Select(r => r.RoomNumber).ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        void AddRoom(string number, int instructionTypeId)
        {
            if (existingSet.Contains(number))
                return;

            db.Rooms.Add(new Room { RoomNumber = number, InstructionTypeId = instructionTypeId });
            existingSet.Add(number);
        }

        AddRoom("A1", 1);
        AddRoom("A2", 1);
        AddRoom("A3", 1);
        AddRoom("101W", 2);
        AddRoom("102W", 2);
        AddRoom("103W", 2);
        AddRoom("104W", 2);
        AddRoom("105W", 2);
        AddRoom("106W", 2);
        AddRoom("L101", 3);
        AddRoom("L102", 3);
        AddRoom("L103", 3);
        AddRoom("L104", 3);
        AddRoom("L105", 3);
        AddRoom("L106", 3);

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    internal static async Task EnsureSemesterPeriodsAsync(
        ApplicationDbContext db,
        string academicYear,
        CancellationToken ct)
    {
        var exists = await db.SemesterPeriods.AnyAsync(p => p.AcademicYear == academicYear, ct);
        if (exists)
            return;

        db.SemesterPeriods.AddRange(
            new SemesterPeriod
            {
                AcademicYear = academicYear,
                SemesterOrdinal = 1,
                StartDate = new DateOnly(2025, 10, 1),
                EndDate = new DateOnly(2026, 2, 15)
            },
            new SemesterPeriod
            {
                AcademicYear = academicYear,
                SemesterOrdinal = 2,
                StartDate = new DateOnly(2026, 2, 16),
                EndDate = new DateOnly(2026, 6, 30)
            });

        await db.SaveChangesAsync(ct);
    }

    internal static async Task EnsurePolishNonWorkingDaysAsync(
        ApplicationDbContext db,
        string academicYear,
        CancellationToken ct)
    {
        if (await db.NonWorkingDays.AnyAsync(d => d.AcademicYear == academicYear, ct))
            return;

        db.NonWorkingDays.AddRange(
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2025, 11, 11), Name = "Święto Niepodległości" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2025, 12, 24), Name = "Wigilia" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2025, 12, 25), Name = "Boże Narodzenie" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2025, 12, 26), Name = "Boże Narodzenie (drugi dzień)" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2026, 1, 1), Name = "Nowy Rok" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2026, 1, 6), Name = "Trzech Króli" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2026, 4, 6), Name = "Poniedziałek Wielkanocny" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2026, 5, 1), Name = "Święto Pracy" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2026, 5, 3), Name = "Święto Konstytucji 3 Maja" },
            new NonWorkingDay { AcademicYear = academicYear, Date = new DateOnly(2026, 6, 4), Name = "Boże Ciało" });

        await db.SaveChangesAsync(ct);
    }

    internal static async Task<Dictionary<string, int>> EnsureLecturersAsync(
        ApplicationDbContext db,
        IReadOnlyList<LecturerDef> lecturers,
        CancellationToken ct)
    {
        var existing = await db.Lecturers.AsNoTracking().ToListAsync(ct);
        var byEmail = existing.ToDictionary(l => l.Email, l => l.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var def in lecturers)
        {
            if (byEmail.ContainsKey(def.Email))
                continue;

            var lecturer = new Lecturer
            {
                FirstName = def.FirstName,
                LastName = def.LastName,
                Email = def.Email
            };
            db.Lecturers.Add(lecturer);
            await db.SaveChangesAsync(ct);
            byEmail[def.Email] = lecturer.Id;
        }

        return byEmail;
    }

    internal static async Task EnsureFullAvailabilityAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<int> lecturerIds,
        IReadOnlyList<TimeSlot> slots,
        CancellationToken ct)
    {
        if (lecturerIds.Count == 0)
            return;

        var lecturerIdSet = lecturerIds.ToHashSet();
        var existing = await db.LecturerAvailabilities
            .Where(a => lecturerIdSet.Contains(a.LecturerId))
            .Select(a => new { a.LecturerId, a.DayOfWeek, a.TimeSlotId })
            .ToListAsync(ct);
        var existingSet = existing
            .Select(a => (a.LecturerId, a.DayOfWeek, a.TimeSlotId))
            .ToHashSet();

        foreach (var lecturerId in lecturerIdSet)
        {
            foreach (var day in Weekdays)
            {
                foreach (var slot in slots)
                {
                    if (existingSet.Contains((lecturerId, day, slot.Id)))
                        continue;

                    db.LecturerAvailabilities.Add(new LecturerAvailability
                    {
                        LecturerId = lecturerId,
                        DayOfWeek = day,
                        TimeSlotId = slot.Id
                    });
                }
            }
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(ct);
    }

    internal static async Task AddSubjectWithLecturersAsync(
        ApplicationDbContext db,
        int programId,
        SubjectDef def,
        IReadOnlyDictionary<string, int> lecturerIds,
        CancellationToken ct)
    {
        if (!lecturerIds.TryGetValue(def.PrimaryEmail, out var primaryId))
            return;

        var subject = new Subject
        {
            StudyProgramId = programId,
            Name = def.Name,
            Semester = def.Semester,
            NumberOfSessions = def.Sessions,
            InstructionTypeId = def.InstructionTypeId
        };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync(ct);

        db.SubjectLecturers.Add(new SubjectLecturer
        {
            SubjectId = subject.Id,
            LecturerId = primaryId,
            IsPrimary = true
        });

        if (def.SecondaryEmail is not null
            && lecturerIds.TryGetValue(def.SecondaryEmail, out var secondaryId)
            && secondaryId != primaryId)
        {
            db.SubjectLecturers.Add(new SubjectLecturer
            {
                SubjectId = subject.Id,
                LecturerId = secondaryId,
                IsPrimary = false
            });
        }
    }

    internal static async Task RepairLecturerAssignmentsAsync(
        ApplicationDbContext db,
        int programId,
        IReadOnlyList<SubjectDef> subjects,
        IReadOnlyDictionary<string, int> lecturerIds,
        CancellationToken ct)
    {
        var dbSubjects = await db.Subjects
            .Include(s => s.SubjectLecturers)
            .Where(s => s.StudyProgramId == programId)
            .ToListAsync(ct);

        var subjectByKey = dbSubjects.ToDictionary(s => (s.Semester, s.Name));

        foreach (var def in subjects)
        {
            if (!subjectByKey.TryGetValue((def.Semester, def.Name), out var subject))
                continue;

            if (!lecturerIds.TryGetValue(def.PrimaryEmail, out var primaryId))
                continue;

            db.SubjectLecturers.RemoveRange(subject.SubjectLecturers);
            subject.SubjectLecturers.Clear();

            db.SubjectLecturers.Add(new SubjectLecturer
            {
                SubjectId = subject.Id,
                LecturerId = primaryId,
                IsPrimary = true
            });

            if (def.SecondaryEmail is not null
                && lecturerIds.TryGetValue(def.SecondaryEmail, out var secondaryId)
                && secondaryId != primaryId)
            {
                db.SubjectLecturers.Add(new SubjectLecturer
                {
                    SubjectId = subject.Id,
                    LecturerId = secondaryId,
                    IsPrimary = false
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    internal static async Task RemoveLegacyProgramsAsync(
        ApplicationDbContext db,
        IReadOnlyList<string> programNames,
        CancellationToken ct)
    {
        var legacyIds = await db.StudyPrograms
            .Where(p => programNames.Contains(p.Name))
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (legacyIds.Count == 0)
            return;

        var legacySubjectIds = await db.Subjects
            .Where(s => legacyIds.Contains(s.StudyProgramId))
            .Select(s => s.Id)
            .ToListAsync(ct);

        var blockingSessions = await db.ScheduledSessions
            .Where(s => legacyIds.Contains(s.StudyProgramId)
                        || legacySubjectIds.Contains(s.SubjectId))
            .ToListAsync(ct);

        if (blockingSessions.Count > 0)
        {
            db.ScheduledSessions.RemoveRange(blockingSessions);
            await db.SaveChangesAsync(ct);
        }

        var legacy = await db.StudyPrograms
            .Where(p => legacyIds.Contains(p.Id))
            .ToListAsync(ct);

        db.StudyPrograms.RemoveRange(legacy);
        await db.SaveChangesAsync(ct);
    }
}
