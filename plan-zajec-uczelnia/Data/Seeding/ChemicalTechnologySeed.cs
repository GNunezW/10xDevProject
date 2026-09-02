using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Models;

namespace plan_zajec_uczelnia.Data.Seeding;

/// <summary>
/// Przykładowe dane kierunku Technologia chemiczna (inż., stacjonarne) — 7 semestrów,
/// prowadzący z pełną dostępnością pn–pt. Idempotentne: pomija seed, gdy kierunek już istnieje.
/// </summary>
public static class ChemicalTechnologySeed
{
    public const string ProgramName = "Technologia chemiczna";
    public const string AcademicYear = "2025/2026";

    private static readonly string[] LegacyTestProgramNames = ["Matematyka"];

    private static readonly TimeOnly[] StandardSlotStarts =
    [
        new(8, 0),
        new(9, 45),
        new(11, 30),
        new(13, 15),
        new(15, 0),
        new(16, 45),
        new(18, 30)
    ];

    private static readonly DayOfWeek[] Weekdays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    private record LecturerDef(string FirstName, string LastName, string Email);

    private record SubjectDef(
        int Semester,
        string Name,
        int InstructionTypeId,
        int Sessions,
        string PrimaryEmail,
        string? SecondaryEmail = null);

    private static readonly LecturerDef[] Lecturers =
    [
        new("Jan", "Kowalski", "j.kowalski@chem.demo.pl"),
        new("Anna", "Nowak", "a.nowak@chem.demo.pl"),
        new("Piotr", "Wiśniewski", "p.wisniewski@chem.demo.pl"),
        new("Maria", "Zielińska", "m.zielinska@chem.demo.pl"),
        new("Tomasz", "Lewandowski", "t.lewandowski@chem.demo.pl"),
        new("Marcin", "Lis", "m.lis@chem.demo.pl"),
        new("Barbara", "Król", "b.krol@chem.demo.pl"),
        new("Karolina", "Pawlak", "k.pawlak@chem.demo.pl"),
        new("Michał", "Kamiński", "m.kaminski@chem.demo.pl"),
        new("Katarzyna", "Wójcik", "k.wojcik@chem.demo.pl"),
        new("Ewa", "Dąbrowska", "e.dabrowska@chem.demo.pl"),
        new("Andrzej", "Mazur", "a.mazur@chem.demo.pl"),
        new("Joanna", "Szymańska", "j.szymanska@chem.demo.pl"),
        new("Robert", "Górski", "r.gorski@chem.demo.pl"),
        new("Wojciech", "Rutkowski", "w.rutkowski@chem.demo.pl"),
        new("Grzegorz", "Jankowski", "g.jankowski@chem.demo.pl"),
        new("Stanisław", "Kozłowski", "s.kozlowski@chem.demo.pl"),
        new("Natalia", "Michalska", "n.michalska@chem.demo.pl"),
        new("Paweł", "Ostrowski", "p.ostrowski@chem.demo.pl"),
        new("Krzysztof", "Bielski", "k.bielski@chem.demo.pl"),
        new("Agnieszka", "Czarnecka", "a.czarnecka@chem.demo.pl"),
        new("Monika", "Adamska", "m.adamska@chem.demo.pl"),
        new("Jakub", "Wróbel", "j.wrobel@chem.demo.pl"),
        new("Tadeusz", "Malinowski", "t.malinowski@chem.demo.pl"),
        new("Magdalena", "Wiśniewska", "m.wisniewska@chem.demo.pl"),
        new("Henryk", "Orłowski", "h.orlowski@chem.demo.pl"),
        new("Olga", "Sienkiewicz", "o.sienkiewicz@chem.demo.pl"),
        new("Laura", "Nowicka", "l.nowicka@chem.demo.pl"),
        new("Leszek", "Piotrowski", "l.piotrowski@chem.demo.pl"),
        new("Iwona", "Grabowska", "i.grabowska@chem.demo.pl"),
        new("Zofia", "Kaczmarek", "z.kaczmarek@chem.demo.pl"),
        new("Damian", "Witczak", "d.witczak@chem.demo.pl"),
        new("Elżbieta", "Markiewicz", "e.markiewicz@chem.demo.pl"),
        new("Filip", "Górecki", "f.gorecki@chem.demo.pl"),
        new("Marek", "Zalewski", "m.zalewski@chem.demo.pl")
    ];

    // Plan inspirowany programem inż. Technologia chemiczna (PŁ / Politechnika Śląska).
    // NumberOfSessions = bloki 90 min w semestrze (≈ 15 tygodni × bloki/tydz.).
    private static readonly SubjectDef[] Subjects =
    [
        // Semestr 1
        new(1, "Matematyka", 1, 15, "j.kowalski@chem.demo.pl"),
        new(1, "Matematyka — ćwiczenia", 2, 30, "a.nowak@chem.demo.pl"),
        new(1, "Fizyka", 1, 15, "p.wisniewski@chem.demo.pl"),
        new(1, "Chemia ogólna i nieorganiczna", 1, 15, "m.zielinska@chem.demo.pl"),
        new(1, "Chemia ogólna i nieorganiczna — laboratorium", 3, 30, "t.lewandowski@chem.demo.pl"),
        new(1, "Informatyka i podstawy programowania", 2, 15, "m.lis@chem.demo.pl"),
        new(1, "Grafika inżynierska i CAD", 3, 30, "b.krol@chem.demo.pl"),
        new(1, "Język angielski", 2, 15, "k.pawlak@chem.demo.pl"),

        // Semestr 2
        new(2, "Matematyka 2", 1, 15, "t.malinowski@chem.demo.pl"),
        new(2, "Matematyka 2 — ćwiczenia", 2, 30, "m.wisniewska@chem.demo.pl"),
        new(2, "Fizyka — laboratorium", 3, 15, "p.wisniewski@chem.demo.pl"),
        new(2, "Chemia nieorganiczna", 1, 15, "m.kaminski@chem.demo.pl"),
        new(2, "Chemia nieorganiczna — laboratorium", 3, 30, "h.orlowski@chem.demo.pl"),
        new(2, "Chemia organiczna I", 1, 15, "k.wojcik@chem.demo.pl"),
        new(2, "Chemia organiczna I — laboratorium", 3, 30, "k.wojcik@chem.demo.pl"),
        new(2, "Wprowadzenie do przedsiębiorczości", 1, 15, "r.gorski@chem.demo.pl"),

        // Semestr 3
        new(3, "Chemia organiczna II", 1, 15, "o.sienkiewicz@chem.demo.pl"),
        new(3, "Chemia fizyczna", 1, 15, "e.dabrowska@chem.demo.pl"),
        new(3, "Chemia fizyczna — laboratorium", 3, 30, "e.dabrowska@chem.demo.pl"),
        new(3, "Inżynieria i aparatura chemiczna", 1, 15, "a.mazur@chem.demo.pl"),
        new(3, "Inżynieria i aparatura chemiczna — laboratorium", 3, 30, "j.szymanska@chem.demo.pl"),
        new(3, "Język angielski 2", 2, 15, "l.nowicka@chem.demo.pl"),

        // Semestr 4
        new(4, "Podstawy technologii chemicznej", 1, 15, "w.rutkowski@chem.demo.pl"),
        new(4, "Podstawy technologii chemicznej — laboratorium", 3, 30, "w.rutkowski@chem.demo.pl"),
        new(4, "Chemia analityczna", 1, 15, "g.jankowski@chem.demo.pl"),
        new(4, "Chemia analityczna — laboratorium", 3, 30, "g.jankowski@chem.demo.pl"),
        new(4, "Termodynamika chemiczna", 1, 15, "s.kozlowski@chem.demo.pl"),
        new(4, "Termodynamika chemiczna — ćwiczenia", 2, 15, "s.kozlowski@chem.demo.pl"),

        // Semestr 5
        new(5, "Operacje jednostkowe", 1, 15, "n.michalska@chem.demo.pl"),
        new(5, "Operacje jednostkowe — laboratorium", 3, 30, "n.michalska@chem.demo.pl"),
        new(5, "Inżynieria chemiczna i procesy", 1, 15, "l.piotrowski@chem.demo.pl"),
        new(5, "Inżynieria chemiczna i procesy — laboratorium", 3, 30, "i.grabowska@chem.demo.pl"),
        new(5, "Statystyka i analiza danych", 2, 15, "k.bielski@chem.demo.pl"),
        new(5, "Automatyka procesów", 1, 15, "p.ostrowski@chem.demo.pl"),

        // Semestr 6
        new(6, "Technologia polimerów", 1, 15, "a.czarnecka@chem.demo.pl"),
        new(6, "Technologia polimerów — laboratorium", 3, 30, "a.czarnecka@chem.demo.pl"),
        new(6, "Bezpieczeństwo procesów chemicznych", 1, 15, "m.adamska@chem.demo.pl"),
        new(6, "Technologia ochrony środowiska", 1, 15, "j.wrobel@chem.demo.pl"),
        new(6, "Technologia ochrony środowiska — laboratorium", 3, 15, "j.wrobel@chem.demo.pl"),
        new(6, "Zarządzanie produkcją", 1, 15, "d.witczak@chem.demo.pl"),

        // Semestr 7
        new(7, "Projekt inżynierski", 3, 30, "m.zalewski@chem.demo.pl"),
        new(7, "Seminarium dyplomowe", 2, 15, "z.kaczmarek@chem.demo.pl"),
        new(7, "Przedmiot obieralny — wykład", 1, 15, "e.markiewicz@chem.demo.pl"),
        new(7, "Przedmiot obieralny — laboratorium", 3, 15, "f.gorecki@chem.demo.pl")
    ];

    private static readonly (int Semester, int Students)[] Enrollments =
    [
        (1, 90),
        (2, 88),
        (3, 85),
        (4, 82),
        (5, 78),
        (6, 75),
        (7, 72)
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await RemoveLegacyTestProgramsAsync(db, cancellationToken);

        await EnsureTimeSlotsAsync(db, cancellationToken);
        await EnsureRoomsAsync(db, cancellationToken);
        await EnsureSemesterPeriodsAsync(db, cancellationToken);
        await DemoSeedInfrastructure.EnsurePolishNonWorkingDaysAsync(db, AcademicYear, cancellationToken);

        var lecturerIds = await EnsureLecturersAsync(db, cancellationToken);
        var slots = await db.TimeSlots.OrderBy(s => s.StartTime).ToListAsync(cancellationToken);
        await EnsureFullAvailabilityAsync(db, lecturerIds, slots, cancellationToken);

        var existingProgram = await db.StudyPrograms
            .FirstOrDefaultAsync(p => p.Name == ProgramName, cancellationToken);

        if (existingProgram is not null)
        {
            await RepairLecturerAssignmentsAsync(db, existingProgram.Id, lecturerIds, cancellationToken);
            return;
        }

        var program = new StudyProgram
        {
            Name = ProgramName,
            StudyMode = StudyMode.FullTime,
            StudyDegree = StudyDegree.FirstDegreeEngineer,
            AcademicYear = AcademicYear
        };
        db.StudyPrograms.Add(program);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var (semester, students) in Enrollments)
        {
            db.StudyProgramEnrollments.Add(new StudyProgramEnrollment
            {
                StudyProgramId = program.Id,
                Semester = semester,
                StudentCount = students
            });
        }

        foreach (var def in Subjects)
        {
            await AddSubjectWithLecturersAsync(db, program.Id, def, lecturerIds, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RepairLecturerAssignmentsAsync(
        ApplicationDbContext db,
        int programId,
        IReadOnlyDictionary<string, int> lecturerIds,
        CancellationToken ct)
    {
        var subjects = await db.Subjects
            .Include(s => s.SubjectLecturers)
            .Where(s => s.StudyProgramId == programId)
            .ToListAsync(ct);

        var subjectByKey = subjects.ToDictionary(s => (s.Semester, s.Name));

        foreach (var def in Subjects)
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

    private static async Task AddSubjectWithLecturersAsync(
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

    private static async Task EnsureTimeSlotsAsync(ApplicationDbContext db, CancellationToken ct)
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

    private static async Task EnsureRoomsAsync(ApplicationDbContext db, CancellationToken ct)
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

    private static async Task EnsureSemesterPeriodsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var exists = await db.SemesterPeriods
            .AnyAsync(p => p.AcademicYear == AcademicYear, ct);
        if (exists)
            return;

        db.SemesterPeriods.AddRange(
            new SemesterPeriod
            {
                AcademicYear = AcademicYear,
                SemesterOrdinal = 1,
                StartDate = new DateOnly(2025, 10, 1),
                EndDate = new DateOnly(2026, 2, 15)
            },
            new SemesterPeriod
            {
                AcademicYear = AcademicYear,
                SemesterOrdinal = 2,
                StartDate = new DateOnly(2026, 2, 16),
                EndDate = new DateOnly(2026, 6, 30)
            });

        await db.SaveChangesAsync(ct);
    }

    private static async Task<Dictionary<string, int>> EnsureLecturersAsync(
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var existing = await db.Lecturers.AsNoTracking().ToListAsync(ct);
        var byEmail = existing.ToDictionary(l => l.Email, l => l.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var def in Lecturers)
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

    private static async Task EnsureFullAvailabilityAsync(
        ApplicationDbContext db,
        IReadOnlyDictionary<string, int> lecturerIds,
        IReadOnlyList<TimeSlot> slots,
        CancellationToken ct)
    {
        var lecturerIdSet = lecturerIds.Values.ToHashSet();
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

    private static async Task RemoveLegacyTestProgramsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var legacyIds = await db.StudyPrograms
            .Where(p => LegacyTestProgramNames.Contains(p.Name))
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
