using System.Text;
using Google.OrTools.Sat;

namespace plan_zajec_uczelnia.Services.Scheduling;

public static class ScheduleInfeasibilityDiagnostics
{
    private const int MaxIssues = 12;

    public static string BuildMessage(
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<int, TimeOnly> slotStartById,
        IReadOnlyDictionary<int, string>? lecturerNamesById = null,
        CpSolverStatus? solverStatus = null)
    {
        var issues = CollectIssues(tasks, slotStartById, lecturerNamesById, solverStatus);
        if (issues.Count == 0)
            return "Nie znaleziono planu spełniającego ograniczenia. Sprawdź dostępność prowadzących, sale i liczbę grup — solver nie wskazał pojedynczej przyczyny.";

        var sb = new StringBuilder();
        sb.AppendLine("Nie znaleziono planu spełniającego ograniczenia. Wykryte kolizje i braki:");
        foreach (var issue in issues.Take(MaxIssues))
            sb.AppendLine($"• {issue}");
        if (issues.Count > MaxIssues)
            sb.AppendLine($"• … i {issues.Count - MaxIssues} innych — zawęź dane lub zwiększ dostępność.");

        return sb.ToString().TrimEnd();
    }

    private static List<string> CollectIssues(
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<int, TimeOnly> slotStartById,
        IReadOnlyDictionary<int, string>? lecturerNamesById,
        CpSolverStatus? solverStatus)
    {
        var issues = new List<string>();

        if (solverStatus is CpSolverStatus.Unknown or CpSolverStatus.ModelInvalid)
        {
            issues.Add(
                "Solver nie zdążył znaleźć planu w limicie czasu (60 s) — spróbuj zmniejszyć liczbę przedmiotów/sesji lub zwiększ dostępność prowadzących.");
        }
        else if (solverStatus == CpSolverStatus.Infeasible)
        {
            issues.Add("Solver uznał zadanie za niewykonalne przy obecnych ograniczeniach (prowadzący, sale, brak okienek, max 2 jednostki z rzędu).");
        }

        AddSummaryIssues(issues, tasks, lecturerNamesById);
        AddConsecutiveSessionIssues(tasks, issues);

        foreach (var task in tasks.Where(t => t.AllowedAssignments.Count == 0))
            issues.Add($"Brak terminu: {SessionLabel(task)} — żaden przypisany prowadzący nie ma dostępności lub brak sal tego typu w dozwolonych dniach.");

        foreach (var task in tasks.Where(t => t.AllowedAssignments.Count == 1))
            issues.Add($"Jedyny możliwy termin: {SessionLabel(task)} → {FormatAssignment(task.AllowedAssignments[0], slotStartById)}.");

        foreach (var subjectGroup in tasks.GroupBy(t => t.SubjectId))
        {
            var withoutSubstitute = subjectGroup
                .Where(t => EligibleLecturerIds(t).Count == 1)
                .ToList();
            if (withoutSubstitute.Count == 0)
                continue;

            var sample = withoutSubstitute[0];
            var lecturerId = EligibleLecturerIds(sample).First();
            issues.Add(
                $"Brak zastępcy przy „{sample.SubjectName}” ({sample.StudyProgramName}): {withoutSubstitute.Count} sesji ma tylko {FormatLecturer(lecturerId, lecturerNamesById)} z dostępnością.");
        }

        foreach (var subjectGroup in tasks.GroupBy(t => t.SubjectId))
        {
            var sample = subjectGroup.First();
            var sessionCount = subjectGroup.Count();
            var lecturerCapacity = subjectGroup
                .SelectMany(t => t.AllowedAssignments)
                .GroupBy(a => a.LecturerId)
                .Select(g => (
                    LecturerId: g.Key,
                    Slots: g.Select(a => (a.Day, a.TimeSlotId)).Distinct().Count()))
                .OrderByDescending(x => x.Slots)
                .ToList();

            if (lecturerCapacity.Count <= 1)
                continue;

            var summary = string.Join(
                ", ",
                lecturerCapacity.Select(l =>
                    $"{FormatLecturer(l.LecturerId, lecturerNamesById)} ({l.Slots} terminów)"));

            if (sessionCount > lecturerCapacity.Max(l => l.Slots))
            {
                issues.Add(
                    $"Przedmiot „{sample.SubjectName}” ({sample.StudyProgramName}): {sessionCount} sesji; dostępni prowadzący: {summary}. Solver rozłoży je między prowadzących.");
            }
        }

        foreach (var lecturerId in tasks
                     .SelectMany(t => t.AllowedAssignments)
                     .Select(a => a.LecturerId)
                     .Distinct())
        {
            var mandatory = tasks.Count(t =>
            {
                var eligible = EligibleLecturerIds(t);
                return eligible.Count == 1 && eligible.First() == lecturerId;
            });
            var slots = tasks
                .SelectMany(t => t.AllowedAssignments)
                .Where(a => a.LecturerId == lecturerId)
                .Select(a => (a.Day, a.TimeSlotId))
                .Distinct()
                .Count();
            if (mandatory > slots)
            {
                issues.Add(
                    $"Przeciążenie prowadzącego {FormatLecturer(lecturerId, lecturerNamesById)}: {mandatory} sesji wymaga wyłącznie tego prowadzącego, a ma tylko {slots} dostępnych terminów (dzień + slot).");
            }
        }

        AddMandatoryLecturerSlotConflicts(issues, tasks, slotStartById, lecturerNamesById);
        AddRoomCapacityIssues(issues, tasks, slotStartById);

        AddPairwiseExclusionIssues(issues, tasks, slotStartById);

        return issues.Distinct().ToList();
    }

    private static void AddSummaryIssues(
        List<string> issues,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<int, string>? lecturerNamesById)
    {
        var totalSessions = tasks.Count;
        var roomSlots = tasks
            .SelectMany(t => t.AllowedAssignments)
            .Select(a => (a.RoomNumber, a.Day, a.TimeSlotId))
            .Distinct()
            .Count();
        var lecturerSlots = tasks
            .SelectMany(t => t.AllowedAssignments)
            .Select(a => (a.LecturerId, a.Day, a.TimeSlotId))
            .Distinct()
            .Count();
        var avgOptions = tasks.Count == 0 ? 0 : tasks.Average(t => t.AllowedAssignments.Count);

        issues.Add(
            $"Podsumowanie: {totalSessions} sesji do zaplanowania, {roomSlots} kombinacji sala+termin, {lecturerSlots} kombinacji prowadzący+termin, średnio {avgOptions:F0} opcji na sesję.");

        foreach (var programGroup in tasks.GroupBy(t => t.StudyProgramId))
        {
            var sample = programGroup.First();
            var subjectCount = programGroup.Select(t => t.SubjectId).Distinct().Count();
            issues.Add(
                $"Kierunek „{sample.StudyProgramName}”: {programGroup.Count()} sesji, {subjectCount} przedmiotów.");
        }
    }

    private static void AddConsecutiveSessionIssues(IReadOnlyList<PlacementTask> tasks, List<string> issues)
    {
        foreach (var group in tasks.GroupBy(t => (t.SubjectId, t.GroupIndex)))
        {
            var count = group.Count();
            if (count <= 2)
                continue;

            var sample = group.First();
            issues.Add(
                $"„{sample.SubjectName}” (gr. {sample.GroupIndex}): {count} bloków tygodniowych — reguła max 2 z rzędu wymaga rozłożenia na wiele dni/slotów.");
        }
    }

    private static HashSet<string> EligibleRoomNumbers(PlacementTask task) =>
        task.AllowedAssignments.Select(a => a.RoomNumber).ToHashSet();

    private static void AddRoomCapacityIssues(
        List<string> issues,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<int, TimeOnly> slotStartById)
    {
        foreach (var subjectGroup in tasks.GroupBy(t => t.SubjectId))
        {
            var sample = subjectGroup.First();
            var sessionCount = subjectGroup.Count();
            var roomNumbers = subjectGroup
                .SelectMany(t => t.AllowedAssignments)
                .Select(a => a.RoomNumber)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
            var roomSlotCombos = subjectGroup
                .SelectMany(t => t.AllowedAssignments)
                .Select(a => (a.RoomNumber, a.Day, a.TimeSlotId))
                .Distinct()
                .Count();

            if (roomNumbers.Count == 0)
                continue;

            if (roomNumbers.Count == 1)
            {
                issues.Add(
                    $"Przedmiot „{sample.SubjectName}” ({sample.StudyProgramName}): jedyna dostępna sala to {roomNumbers[0]} — {sessionCount} sesji na {roomSlotCombos} terminów sala+slot.");
            }

            if (sessionCount > roomSlotCombos)
            {
                var roomList = roomNumbers.Count <= 6
                    ? string.Join(", ", roomNumbers)
                    : $"{string.Join(", ", roomNumbers.Take(5))} (+{roomNumbers.Count - 5})";
                issues.Add(
                    $"Za mało terminów: „{sample.SubjectName}” ({sample.StudyProgramName}) — {sessionCount} sesji, {roomSlotCombos} kombinacji sala+slot (sale: {roomList}).");
            }
        }

        AddMandatoryRoomSlotConflicts(issues, tasks, slotStartById);
    }

    private static void AddMandatoryLecturerSlotConflicts(
        List<string> issues,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<int, TimeOnly> slotStartById,
        IReadOnlyDictionary<int, string>? lecturerNamesById)
    {
        var buckets = new Dictionary<string, (int LecturerId, DayOfWeek Day, int SlotId, List<PlacementTask> Tasks)>();

        foreach (var task in tasks)
        {
            var eligible = EligibleLecturerIds(task);
            if (eligible.Count != 1)
                continue;

            var lecturerId = eligible.First();
            foreach (var assignment in task.AllowedAssignments.Where(a => a.LecturerId == lecturerId))
            {
                var key = $"{lecturerId}|{(int)assignment.Day}|{assignment.TimeSlotId}";
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = (lecturerId, assignment.Day, assignment.TimeSlotId, []);
                    buckets[key] = bucket;
                }

                if (!bucket.Tasks.Contains(task))
                    bucket.Tasks.Add(task);
            }
        }

        foreach (var bucket in buckets.Values.Where(b => b.Tasks.Count > 1).OrderByDescending(b => b.Tasks.Count))
        {
            var time = FormatTime(bucket.SlotId, slotStartById);
            var header =
                $"Konflikt — {FormatLecturer(bucket.LecturerId, lecturerNamesById)} {DayLabel(bucket.Day)} {time} (sesje bez innego prowadzącego):";
            var names = bucket.Tasks.Select(SessionLabel).Take(5);
            var tail = bucket.Tasks.Count > 5 ? $" (+{bucket.Tasks.Count - 5})" : "";
            issues.Add($"{header} {string.Join("; ", names)}{tail}.");
        }
    }

    private static void AddMandatoryRoomSlotConflicts(
        List<string> issues,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<int, TimeOnly> slotStartById)
    {
        var buckets = new Dictionary<string, (string RoomNumber, DayOfWeek Day, int SlotId, List<PlacementTask> Tasks)>();

        foreach (var task in tasks)
        {
            if (EligibleRoomNumbers(task).Count != 1)
                continue;

            var roomNumber = EligibleRoomNumbers(task).First();
            foreach (var assignment in task.AllowedAssignments.Where(a => a.RoomNumber == roomNumber))
            {
                var key = $"{roomNumber}|{(int)assignment.Day}|{assignment.TimeSlotId}";
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = (roomNumber, assignment.Day, assignment.TimeSlotId, []);
                    buckets[key] = bucket;
                }

                if (!bucket.Tasks.Contains(task))
                    bucket.Tasks.Add(task);
            }
        }

        foreach (var bucket in buckets.Values.Where(b => b.Tasks.Count > 1).OrderByDescending(b => b.Tasks.Count))
        {
            var time = FormatTime(bucket.SlotId, slotStartById);
            var header =
                $"Konflikt sali {bucket.RoomNumber} — {DayLabel(bucket.Day)} {time} (sesje bez innej sali):";
            var names = bucket.Tasks.Select(SessionLabel).Take(5);
            var tail = bucket.Tasks.Count > 5 ? $" (+{bucket.Tasks.Count - 5})" : "";
            issues.Add($"{header} {string.Join("; ", names)}{tail}.");
        }
    }

    private static HashSet<int> EligibleLecturerIds(PlacementTask task) =>
        task.AllowedAssignments.Select(a => a.LecturerId).ToHashSet();

    private static string FormatLecturer(int lecturerId, IReadOnlyDictionary<int, string>? lecturerNamesById) =>
        lecturerNamesById is not null && lecturerNamesById.TryGetValue(lecturerId, out var name)
            ? name
            : $"ID {lecturerId}";

    private static void AddPairwiseExclusionIssues(
        List<string> issues,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<int, TimeOnly> slotStartById)
    {
        var reported = 0;
        for (var i = 0; i < tasks.Count && reported < 5; i++)
        {
            for (var j = i + 1; j < tasks.Count && reported < 5; j++)
            {
                if (!AreMutuallyExclusive(tasks[i], tasks[j]))
                    continue;

                issues.Add(
                    $"Sesje wykluczają się (brak wspólnego terminu): {SessionLabel(tasks[i])} ↔ {SessionLabel(tasks[j])}.");
                reported++;
            }
        }
    }

    private static bool AreMutuallyExclusive(PlacementTask a, PlacementTask b)
    {
        foreach (var aa in a.AllowedAssignments)
        {
            foreach (var bb in b.AllowedAssignments)
            {
                if (!AssignmentsConflict(a, aa, b, bb))
                    return false;
            }
        }

        return true;
    }

    private static bool AssignmentsConflict(
        PlacementTask t1,
        ScheduleAssignment a1,
        PlacementTask t2,
        ScheduleAssignment a2)
    {
        if (a1.LecturerId == a2.LecturerId
            && a1.Day == a2.Day
            && a1.TimeSlotId == a2.TimeSlotId)
            return true;

        if (a1.RoomNumber == a2.RoomNumber
            && a1.Day == a2.Day
            && a1.TimeSlotId == a2.TimeSlotId)
            return true;

        return false;
    }

    private static string SessionLabel(PlacementTask t) =>
        $"{t.SubjectName} ({t.StudyProgramName}, gr. {t.GroupIndex}, blok tyg. {t.SessionIndex})";

    private static string FormatAssignment(
        ScheduleAssignment a,
        IReadOnlyDictionary<int, TimeOnly> slotStartById) =>
        $"{DayLabel(a.Day)} {FormatTime(a.TimeSlotId, slotStartById)}, sala {a.RoomNumber}";

    private static string FormatTime(int slotId, IReadOnlyDictionary<int, TimeOnly> slotStartById) =>
        slotStartById.TryGetValue(slotId, out var time) ? time.ToString("HH\\:mm") : $"slot #{slotId}";

    private static string DayLabel(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "pon.",
        DayOfWeek.Tuesday => "wt.",
        DayOfWeek.Wednesday => "śr.",
        DayOfWeek.Thursday => "czw.",
        DayOfWeek.Friday => "pt.",
        DayOfWeek.Saturday => "sob.",
        DayOfWeek.Sunday => "nd.",
        _ => day.ToString()
    };
}
