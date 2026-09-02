using Microsoft.EntityFrameworkCore;
using plan_zajec_uczelnia.Models;
using static plan_zajec_uczelnia.Data.Seeding.DemoSeedInfrastructure;

namespace plan_zajec_uczelnia.Data.Seeding;

/// <summary>
/// Informatyka — I stopień inżynierskie, stacjonarne, 7 semestrów.
/// Plan inspirowany programami PUT / UKSW (2024/25). Idempotentne.
/// </summary>
public static class ComputerScienceSeed
{
    public const string ProgramName = "Informatyka";
    public const string AcademicYear = "2025/2026";

    private static readonly LecturerDef[] Lecturers =
    [
        new("Jan", "Kowalczyk", "j.kowalczyk@inf.demo.pl"),
        new("Anna", "Nowicka", "a.nowicka@inf.demo.pl"),
        new("Piotr", "Wiśniewski", "p.wisniewski@inf.demo.pl"),
        new("Maria", "Zielińska", "m.zielinska@inf.demo.pl"),
        new("Andrzej", "Mazur", "a.mazur@inf.demo.pl"),
        new("Marcin", "Lis", "m.lis@inf.demo.pl"),
        new("Barbara", "Król", "b.krol@inf.demo.pl"),
        new("Karolina", "Pawlak", "k.pawlak@inf.demo.pl"),
        new("Tomasz", "Malinowski", "t.malinowski@inf.demo.pl"),
        new("Ewa", "Dąbrowska", "e.dabrowska@inf.demo.pl"),
        new("Krzysztof", "Bielski", "k.bielski@inf.demo.pl"),
        new("Michał", "Kamiński", "m.kaminski@inf.demo.pl"),
        new("Julia", "Kowalczyk", "julia.kowalczyk@inf.demo.pl"),
        new("Natalia", "Michalska", "n.michalska@inf.demo.pl"),
        new("Grzegorz", "Jankowski", "g.jankowski@inf.demo.pl"),
        new("Joanna", "Szymańska", "j.szymanska@inf.demo.pl"),
        new("Wojciech", "Rutkowski", "w.rutkowski@inf.demo.pl"),
        new("Magdalena", "Wiśniewska", "m.wisniewska@inf.demo.pl"),
        new("Paweł", "Ostrowski", "p.ostrowski@inf.demo.pl"),
        new("Jakub", "Wróbel", "j.wrobel@inf.demo.pl"),
        new("Agnieszka", "Czarnecka", "a.czarnecka@inf.demo.pl"),
        new("Filip", "Górecki", "f.gorecki@inf.demo.pl"),
        new("Leszek", "Piotrowski", "l.piotrowski@inf.demo.pl"),
        new("Stanisław", "Kozłowski", "s.kozlowski@inf.demo.pl"),
        new("Henryk", "Orłowski", "h.orlowski@inf.demo.pl"),
        new("Olga", "Sienkiewicz", "o.sienkiewicz@inf.demo.pl"),
        new("Iwona", "Grabowska", "i.grabowska@inf.demo.pl"),
        new("Marek", "Zalewski", "m.zalewski@inf.demo.pl"),
        new("Robert", "Górski", "r.gorski@inf.demo.pl"),
        new("Damian", "Witczak", "d.witczak@inf.demo.pl"),
        new("Elżbieta", "Markiewicz", "e.markiewicz@inf.demo.pl"),
        new("Zofia", "Kaczmarek", "z.kaczmarek@inf.demo.pl"),
        new("Kamil", "Łuczak", "k.luczak@inf.demo.pl")
    ];

    // NumberOfSessions = bloki 90 min w semestrze (z godzin akademickich ÷ 2).
    private static readonly SubjectDef[] Subjects =
    [
        // —— Semestr 1 (rok I) ——
        new(1, "Analiza matematyczna", 1, 15, "j.kowalczyk@inf.demo.pl"),
        new(1, "Analiza matematyczna — ćwiczenia", 2, 30, "j.kowalczyk@inf.demo.pl"),
        new(1, "Algebra liniowa", 1, 15, "a.nowicka@inf.demo.pl"),
        new(1, "Algebra liniowa — ćwiczenia", 2, 15, "a.nowicka@inf.demo.pl"),
        new(1, "Matematyka dyskretna", 1, 15, "p.wisniewski@inf.demo.pl"),
        new(1, "Matematyka dyskretna — ćwiczenia", 2, 15, "p.wisniewski@inf.demo.pl"),
        new(1, "Logika obliczeniowa", 1, 15, "m.zielinska@inf.demo.pl"),
        new(1, "Logika obliczeniowa — ćwiczenia", 2, 15, "m.zielinska@inf.demo.pl"),
        new(1, "Wprowadzenie do informatyki", 1, 15, "a.mazur@inf.demo.pl"),
        new(1, "Wprowadzenie do informatyki — ćwiczenia", 2, 15, "a.mazur@inf.demo.pl"),
        new(1, "Podstawy programowania", 2, 15, "m.lis@inf.demo.pl"),
        new(1, "Podstawy programowania — laboratorium", 3, 15, "m.lis@inf.demo.pl"),
        new(1, "Narzędzia informatyki", 2, 15, "b.krol@inf.demo.pl"),
        new(1, "Narzędzia informatyki — laboratorium", 3, 15, "b.krol@inf.demo.pl"),
        new(1, "Język angielski", 2, 15, "k.pawlak@inf.demo.pl"),

        // —— Semestr 2 (rok I) ——
        new(2, "Analiza matematyczna II", 1, 15, "t.malinowski@inf.demo.pl"),
        new(2, "Analiza matematyczna II — ćwiczenia", 2, 30, "t.malinowski@inf.demo.pl"),
        new(2, "Programowanie obiektowe", 1, 15, "e.dabrowska@inf.demo.pl"),
        new(2, "Programowanie obiektowe — laboratorium", 3, 15, "e.dabrowska@inf.demo.pl"),
        new(2, "Architektura systemów komputerowych", 1, 15, "k.bielski@inf.demo.pl"),
        new(2, "Architektura systemów komputerowych — laboratorium", 3, 15, "k.bielski@inf.demo.pl"),
        new(2, "Techniki cyfrowe", 1, 15, "m.kaminski@inf.demo.pl"),
        new(2, "Techniki cyfrowe — laboratorium", 3, 15, "m.kaminski@inf.demo.pl"),
        new(2, "Język angielski 2", 2, 15, "julia.kowalczyk@inf.demo.pl"),

        // —— Semestr 3 (rok II) ——
        new(3, "Rachunek prawdopodobieństwa i statystyka", 1, 15, "n.michalska@inf.demo.pl"),
        new(3, "Rachunek prawdopodobieństwa i statystyka — ćwiczenia", 2, 15, "n.michalska@inf.demo.pl"),
        new(3, "Algorytmy i struktury danych", 1, 15, "g.jankowski@inf.demo.pl"),
        new(3, "Algorytmy i struktury danych — laboratorium", 3, 15, "g.jankowski@inf.demo.pl"),
        new(3, "Bazy danych", 1, 15, "j.szymanska@inf.demo.pl"),
        new(3, "Bazy danych — laboratorium", 3, 15, "j.szymanska@inf.demo.pl"),
        new(3, "Inżynieria oprogramowania", 1, 15, "w.rutkowski@inf.demo.pl"),
        new(3, "Inżynieria oprogramowania — laboratorium", 3, 15, "w.rutkowski@inf.demo.pl"),
        new(3, "Język angielski 3", 2, 15, "m.wisniewska@inf.demo.pl"),

        // —— Semestr 4 (rok II) ——
        new(4, "Sieci komputerowe", 1, 15, "p.ostrowski@inf.demo.pl"),
        new(4, "Sieci komputerowe — laboratorium", 3, 15, "p.ostrowski@inf.demo.pl"),
        new(4, "Systemy operacyjne", 1, 15, "j.wrobel@inf.demo.pl"),
        new(4, "Systemy operacyjne — laboratorium", 3, 15, "j.wrobel@inf.demo.pl"),
        new(4, "Kompilatory i tłumacze programów", 1, 15, "a.czarnecka@inf.demo.pl"),
        new(4, "Kompilatory i tłumacze programów — laboratorium", 3, 15, "a.czarnecka@inf.demo.pl"),
        new(4, "Grafika komputerowa", 1, 15, "f.gorecki@inf.demo.pl"),
        new(4, "Grafika komputerowa — laboratorium", 3, 15, "f.gorecki@inf.demo.pl"),
        new(4, "Projekt programistyczny zespołowy", 3, 30, "l.piotrowski@inf.demo.pl"),

        // —— Semestr 5 (rok III) ——
        new(5, "Sztuczna inteligencja", 1, 15, "s.kozlowski@inf.demo.pl"),
        new(5, "Sztuczna inteligencja — laboratorium", 3, 15, "s.kozlowski@inf.demo.pl"),
        new(5, "Bezpieczeństwo systemów komputerowych", 1, 15, "h.orlowski@inf.demo.pl"),
        new(5, "Bezpieczeństwo systemów komputerowych — laboratorium", 3, 15, "h.orlowski@inf.demo.pl"),
        new(5, "Metody numeryczne", 1, 15, "o.sienkiewicz@inf.demo.pl"),
        new(5, "Metody numeryczne — ćwiczenia", 2, 15, "o.sienkiewicz@inf.demo.pl"),
        new(5, "Przedmiot obieralny — uczenie maszynowe", 1, 15, "i.grabowska@inf.demo.pl"),

        // —— Semestr 6 (rok III) ——
        new(6, "Systemy rozproszone", 1, 15, "m.zalewski@inf.demo.pl"),
        new(6, "Systemy rozproszone — laboratorium", 3, 15, "m.zalewski@inf.demo.pl"),
        new(6, "Warsztat inżynierii oprogramowania", 3, 30, "r.gorski@inf.demo.pl"),
        new(6, "Przedmiot obieralny — chmura obliczeniowa", 1, 15, "d.witczak@inf.demo.pl"),
        new(6, "Przedmiot obieralny — chmura obliczeniowa — laboratorium", 3, 15, "d.witczak@inf.demo.pl"),
        new(6, "Przedmiot obieralny — big data", 1, 15, "e.markiewicz@inf.demo.pl"),
        new(6, "Przedmiot obieralny — big data — laboratorium", 3, 15, "e.markiewicz@inf.demo.pl"),

        // —— Semestr 7 (rok IV) ——
        new(7, "Projekt inżynierski", 3, 30, "w.rutkowski@inf.demo.pl"),
        new(7, "Seminarium dyplomowe", 2, 15, "z.kaczmarek@inf.demo.pl"),
        new(7, "Przedmiot obieralny — cyberbezpieczeństwo", 1, 15, "k.luczak@inf.demo.pl")
    ];

    private static readonly (int Semester, int Students)[] Enrollments =
    [
        (1, 120),
        (2, 118),
        (3, 115),
        (4, 112),
        (5, 108),
        (6, 105),
        (7, 102)
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await EnsureTimeSlotsAsync(db, cancellationToken);
        await EnsureRoomsAsync(db, cancellationToken);
        await EnsureSemesterPeriodsAsync(db, AcademicYear, cancellationToken);
        await EnsurePolishNonWorkingDaysAsync(db, AcademicYear, cancellationToken);

        var lecturerIds = await EnsureLecturersAsync(db, Lecturers, cancellationToken);
        var slots = await db.TimeSlots.OrderBy(s => s.StartTime).ToListAsync(cancellationToken);
        await EnsureFullAvailabilityAsync(db, lecturerIds.Values, slots, cancellationToken);

        var existingProgram = await db.StudyPrograms
            .FirstOrDefaultAsync(p => p.Name == ProgramName, cancellationToken);

        if (existingProgram is not null)
        {
            if (existingProgram.StudyDegree != StudyDegree.FirstDegreeEngineer)
            {
                existingProgram.StudyDegree = StudyDegree.FirstDegreeEngineer;
                await db.SaveChangesAsync(cancellationToken);
            }

            await RepairLecturerAssignmentsAsync(db, existingProgram.Id, Subjects, lecturerIds, cancellationToken);
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
            await AddSubjectWithLecturersAsync(db, program.Id, def, lecturerIds, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }
}
