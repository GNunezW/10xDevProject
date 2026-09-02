using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Services;

public sealed class SubjectStaffingContext
{
    public required IReadOnlyDictionary<int, HashSet<(DayOfWeek Day, int TimeSlotId)>> AvailabilityByLecturer { get; init; }
    public required IReadOnlyList<SemesterPeriod> SemesterPeriods { get; init; }
    public required IReadOnlyList<NonWorkingDay> NonWorkingDays { get; init; }
}

public static class SubjectStaffingEvaluator
{
    /// <summary>
    /// Sprawdza, czy przypisani prowadzący mają wystarczającą liczbę slotów tygodniowo,
    /// by poprowadzić wszystkie grupy (w różnych terminach — jak w solverze).
    /// </summary>
    public static SubjectStaffingResult Evaluate(
        Subject subject,
        int studentCount,
        SubjectStaffingContext context)
    {
        if (subject.InstructionType is null)
            return Insufficient(0, 0, subject.SubjectLecturers.Count, 0, "Brak typu zajęć");

        var groups = InstructionType.EstimateGroupCount(studentCount, subject.InstructionType);
        if (groups <= 0)
            return Insufficient(0, 0, subject.SubjectLecturers.Count, 0, "Brak liczebności studentów");

        var assigned = subject.SubjectLecturers.Count;
        if (assigned == 0)
            return Insufficient(0, 0, 0, groups, "Brak przypisanych prowadzących");

        if (subject.StudyProgram is null)
            return Insufficient(0, 0, assigned, groups, "Brak kierunku studiów");

        var (semesterStart, semesterEnd) = ScheduleSemesterBounds.Resolve(
            subject.StudyProgram.AcademicYear, context.SemesterPeriods);
        if (semesterStart is null || semesterEnd is null)
            return Insufficient(0, 0, assigned, groups, "Brak kalendarza semestru dla kierunku");

        var holidays = ScheduleSemesterBounds.ResolveNonWorkingDays(
            subject.StudyProgram.AcademicYear, context.NonWorkingDays);

        var weeklySlotsPerGroup = ScheduleWeekCalculator.ResolveWeeklySlotCount(
            subject.NumberOfSessions,
            semesterStart.Value,
            semesterEnd.Value,
            subject.StudyProgram.StudyMode,
            holidays);
        if (weeklySlotsPerGroup <= 0)
            return Insufficient(0, 0, assigned, groups, "Brak zajęć w semestrze");

        var weeklySessionsNeeded = groups * weeklySlotsPerGroup;
        var allowedDays = ScheduleGridData.GetDaysForMode(subject.StudyProgram.StudyMode).ToHashSet();
        var weeklyCapacity = CountWeeklyCapacity(subject, context, allowedDays);

        var sufficient = weeklyCapacity >= weeklySessionsNeeded;
        var message = sufficient
            ? $"OK — {assigned} prowadzących, {weeklyCapacity} slotów/tydz. (potrzeba {weeklySessionsNeeded}, {groups} grup)"
            : $"Niewystarczająca obsada — {weeklyCapacity} slotów/tydz. na {weeklySessionsNeeded} spotkań ({groups} grup × {weeklySlotsPerGroup}/tydz.)";

        return new SubjectStaffingResult(
            weeklySessionsNeeded,
            weeklyCapacity,
            assigned,
            groups,
            sufficient,
            message);
    }

    private static int CountWeeklyCapacity(
        Subject subject,
        SubjectStaffingContext context,
        HashSet<DayOfWeek> allowedDays)
    {
        var total = 0;
        foreach (var link in subject.SubjectLecturers)
        {
            if (!context.AvailabilityByLecturer.TryGetValue(link.LecturerId, out var slots))
                continue;

            total += slots.Count(slot => allowedDays.Contains(slot.Day));
        }

        return total;
    }

    private static SubjectStaffingResult Insufficient(
        int weeklySessionsNeeded,
        int weeklyCapacity,
        int assigned,
        int groups,
        string reason) =>
        new(weeklySessionsNeeded, weeklyCapacity, assigned, groups, false, reason);
}

public readonly record struct SubjectStaffingResult(
    int WeeklySessionsNeeded,
    int WeeklyCapacitySlots,
    int AssignedLecturers,
    int GroupCount,
    bool IsSufficient,
    string Message);
