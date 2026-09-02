using Google.OrTools.Sat;

namespace plan_zajec_uczelnia.Services.Scheduling;

public sealed class ScheduleCpSatSolver
{
    public ScheduleSolveResult Solve(
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<int, int> slotIndexByTimeSlotId,
        int timeLimitSeconds)
    {
        if (tasks.Any(t => t.AllowedAssignments.Count == 0))
            return ScheduleSolveResult.Failed(CpSolverStatus.Infeasible);

        var model = new CpModel();
        var assignmentVars = new IntVar[tasks.Count];

        for (var t = 0; t < tasks.Count; t++)
            assignmentVars[t] = model.NewIntVar(0, tasks[t].AllowedAssignments.Count - 1, $"a_{t}");

        var picks = CreatePickLiterals(model, tasks, assignmentVars);
        var taskSlotLiterals = CreateTaskSlotLiterals(model, tasks, picks);
        AddAtMostOneResourceConstraints(model, tasks, picks);
        AddStudentCohortSlotConstraints(model, tasks, taskSlotLiterals);
        AddProgramDayAdjacencyConstraints(model, tasks, taskSlotLiterals, slotIndexByTimeSlotId);
        AddMaxConsecutiveSubjectPerDayConstraints(model, tasks, taskSlotLiterals, slotIndexByTimeSlotId);
        AddGapMinimizationObjective(model, tasks, taskSlotLiterals, slotIndexByTimeSlotId, assignmentVars);

        var solver = new CpSolver
        {
            StringParameters = $"max_time_in_seconds:{timeLimitSeconds},num_search_workers:8"
        };

        var status = solver.Solve(model);
        if (status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
        {
            var solution = new List<ScheduledPlacement>(tasks.Count);
            for (var t = 0; t < tasks.Count; t++)
            {
                var choice = tasks[t].AllowedAssignments[(int)solver.Value(assignmentVars[t])];
                solution.Add(new ScheduledPlacement(
                    tasks[t].StudyProgramId,
                    tasks[t].SubjectId,
                    tasks[t].GroupIndex,
                    tasks[t].SessionIndex,
                    choice.LecturerId,
                    choice.Day,
                    choice.TimeSlotId,
                    choice.RoomNumber));
            }

            return ScheduleSolveResult.Succeeded(solution);
        }

        return ScheduleSolveResult.Failed(status);
    }

    private static BoolVar[][] CreatePickLiterals(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        IntVar[] assignmentVars)
    {
        var picks = new BoolVar[tasks.Count][];
        for (var t = 0; t < tasks.Count; t++)
        {
            picks[t] = new BoolVar[tasks[t].AllowedAssignments.Count];
            for (var ai = 0; ai < tasks[t].AllowedAssignments.Count; ai++)
            {
                picks[t][ai] = model.NewBoolVar($"pick_{t}_{ai}");
                model.Add(assignmentVars[t] == ai).OnlyEnforceIf(picks[t][ai]);
                model.Add(assignmentVars[t] != ai).OnlyEnforceIf(picks[t][ai].Not());
            }

            model.AddExactlyOne(picks[t]);
        }

        return picks;
    }

    private static Dictionary<(int TaskIndex, DayOfWeek Day, int SlotId), BoolVar> CreateTaskSlotLiterals(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        BoolVar[][] picks)
    {
        var literals = new Dictionary<(int, DayOfWeek, int), BoolVar>();

        for (var t = 0; t < tasks.Count; t++)
        {
            foreach (var group in tasks[t].AllowedAssignments
                         .Select((assignment, index) => (assignment, index))
                         .GroupBy(x => (x.assignment.Day, x.assignment.TimeSlotId)))
            {
                var key = (t, group.Key.Day, group.Key.TimeSlotId);
                if (literals.ContainsKey(key))
                    continue;

                var pickLiterals = group.Select(x => picks[t][x.index]).ToList();
                var atSlot = model.NewBoolVar($"slot_{t}_{(int)group.Key.Day}_{group.Key.TimeSlotId}");
                LinkBoolOr(model, atSlot, pickLiterals);
                literals[key] = atSlot;
            }
        }

        return literals;
    }

    private static void AddAtMostOneResourceConstraints(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        BoolVar[][] picks)
    {
        var lecturerSlotLiterals = new Dictionary<(int LecturerId, DayOfWeek Day, int SlotId), List<BoolVar>>();
        var roomSlotLiterals = new Dictionary<(string Room, DayOfWeek Day, int SlotId), List<BoolVar>>();

        for (var t = 0; t < tasks.Count; t++)
        {
            for (var ai = 0; ai < tasks[t].AllowedAssignments.Count; ai++)
            {
                var a = tasks[t].AllowedAssignments[ai];
                var pick = picks[t][ai];

                var lectKey = (a.LecturerId, a.Day, a.TimeSlotId);
                if (!lecturerSlotLiterals.TryGetValue(lectKey, out var lectList))
                    lecturerSlotLiterals[lectKey] = lectList = [];
                lectList.Add(pick);

                var roomKey = (a.RoomNumber, a.Day, a.TimeSlotId);
                if (!roomSlotLiterals.TryGetValue(roomKey, out var roomList))
                    roomSlotLiterals[roomKey] = roomList = [];
                roomList.Add(pick);
            }
        }

        foreach (var literals in lecturerSlotLiterals.Values.Where(l => l.Count > 1))
            model.AddAtMostOne(literals);

        foreach (var literals in roomSlotLiterals.Values.Where(l => l.Count > 1))
            model.AddAtMostOne(literals);
    }

    private static void LinkBoolOr(CpModel model, BoolVar result, IReadOnlyList<BoolVar> literals)
    {
        if (literals.Count == 0)
            return;

        if (literals.Count == 1)
        {
            model.Add(result == literals[0]);
            return;
        }

        model.AddBoolOr(literals).OnlyEnforceIf(result);
        model.AddBoolAnd(literals.Select(l => l.Not()).ToArray()).OnlyEnforceIf(result.Not());
        foreach (var literal in literals)
            model.AddImplication(literal, result);
    }

    private static void AddStudentCohortSlotConstraints(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<(int TaskIndex, DayOfWeek Day, int SlotId), BoolVar> taskSlotLiterals)
    {
        var cohortLiterals =
            new Dictionary<(int ProgramId, int Semester, int Group, DayOfWeek Day, int SlotId), List<BoolVar>>();

        for (var t = 0; t < tasks.Count; t++)
        {
            foreach (var slot in tasks[t].AllowedAssignments
                         .Select(a => (a.Day, a.TimeSlotId))
                         .Distinct())
            {
                if (!taskSlotLiterals.TryGetValue((t, slot.Day, slot.TimeSlotId), out var lit))
                    continue;

                var key = (tasks[t].StudyProgramId, tasks[t].Semester, tasks[t].GroupIndex, slot.Day, slot.TimeSlotId);
                if (!cohortLiterals.TryGetValue(key, out var list))
                    cohortLiterals[key] = list = [];
                list.Add(lit);
            }
        }

        foreach (var literals in cohortLiterals.Values.Where(l => l.Count > 1))
            model.AddAtMostOne(literals);
    }

    private static void AddProgramDayAdjacencyConstraints(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<(int TaskIndex, DayOfWeek Day, int SlotId), BoolVar> taskSlotLiterals,
        IReadOnlyDictionary<int, int> slotIndexByTimeSlotId)
    {
        var slotIdsByIndex = slotIndexByTimeSlotId
            .OrderBy(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        for (var i = 0; i < tasks.Count; i++)
        {
            for (var j = i + 1; j < tasks.Count; j++)
            {
                if (tasks[i].StudyProgramId != tasks[j].StudyProgramId)
                    continue;

                if (tasks[i].Semester != tasks[j].Semester)
                    continue;

                if (tasks[i].GroupIndex != tasks[j].GroupIndex)
                    continue;

                if (tasks[i].SubjectId == tasks[j].SubjectId)
                    continue;

                for (var day = DayOfWeek.Monday; day <= DayOfWeek.Sunday; day++)
                {
                    for (var s = 0; s < slotIdsByIndex.Count - 1; s++)
                    {
                        ForbidAdjacentSlots(model, taskSlotLiterals, i, j, day, slotIdsByIndex[s], slotIdsByIndex[s + 1]);
                        ForbidAdjacentSlots(model, taskSlotLiterals, j, i, day, slotIdsByIndex[s], slotIdsByIndex[s + 1]);
                    }
                }
            }
        }
    }

    private static void ForbidAdjacentSlots(
        CpModel model,
        IReadOnlyDictionary<(int TaskIndex, DayOfWeek Day, int SlotId), BoolVar> taskSlotLiterals,
        int earlierTask,
        int laterTask,
        DayOfWeek day,
        int earlierSlotId,
        int laterSlotId)
    {
        if (!taskSlotLiterals.TryGetValue((earlierTask, day, earlierSlotId), out var earlierLit))
            return;
        if (!taskSlotLiterals.TryGetValue((laterTask, day, laterSlotId), out var laterLit))
            return;

        model.AddBoolOr([earlierLit.Not(), laterLit.Not()]);
    }

    private static void AddMaxConsecutiveSubjectPerDayConstraints(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<(int TaskIndex, DayOfWeek Day, int SlotId), BoolVar> taskSlotLiterals,
        IReadOnlyDictionary<int, int> slotIndexByTimeSlotId,
        int maxConsecutive = 2)
    {
        if (maxConsecutive < 1)
            return;

        var windowSize = maxConsecutive + 1;
        var slotIdsByIndex = slotIndexByTimeSlotId
            .OrderBy(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        if (slotIdsByIndex.Count < windowSize)
            return;

        var tasksBySubjectGroup = tasks
            .Select((task, index) => (task, index))
            .GroupBy(x => (x.task.SubjectId, x.task.GroupIndex));

        foreach (var group in tasksBySubjectGroup)
        {
            var taskEntries = group.ToList();
            if (taskEntries.Count < windowSize)
                continue;

            for (var day = DayOfWeek.Monday; day <= DayOfWeek.Sunday; day++)
            {
                for (var start = 0; start <= slotIdsByIndex.Count - windowSize; start++)
                {
                    var windowSlots = slotIdsByIndex.Skip(start).Take(windowSize).ToArray();
                    ForbidThreeConsecutiveTasks(model, taskEntries, taskSlotLiterals, day, windowSlots);
                }
            }
        }
    }

    private static void ForbidThreeConsecutiveTasks(
        CpModel model,
        List<(PlacementTask task, int index)> taskEntries,
        IReadOnlyDictionary<(int TaskIndex, DayOfWeek Day, int SlotId), BoolVar> taskSlotLiterals,
        DayOfWeek day,
        int[] windowSlotIds)
    {
        if (windowSlotIds.Length < 3)
            return;

        for (var a = 0; a < taskEntries.Count; a++)
        {
            for (var b = 0; b < taskEntries.Count; b++)
            {
                if (b == a)
                    continue;
                for (var c = 0; c < taskEntries.Count; c++)
                {
                    if (c == a || c == b)
                        continue;

                    if (!taskSlotLiterals.TryGetValue((taskEntries[a].index, day, windowSlotIds[0]), out var litA))
                        continue;
                    if (!taskSlotLiterals.TryGetValue((taskEntries[b].index, day, windowSlotIds[1]), out var litB))
                        continue;
                    if (!taskSlotLiterals.TryGetValue((taskEntries[c].index, day, windowSlotIds[2]), out var litC))
                        continue;

                    model.AddBoolOr([litA.Not(), litB.Not(), litC.Not()]);
                }
            }
        }
    }

    private static void AddGapMinimizationObjective(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        IReadOnlyDictionary<(int TaskIndex, DayOfWeek Day, int SlotId), BoolVar> taskSlotLiterals,
        IReadOnlyDictionary<int, int> slotIndexByTimeSlotId,
        IntVar[] assignmentVars)
    {
        // Lexicographic: fewest mid-day gaps (PRD), then fewer substitutes, then earlier slots.
        // One gap must beat any mix of substitute + slot costs at MVP scale (~hundreds of sessions).
        const int gapWeight = 1_000_000;
        const int nonPrimaryLecturerPenalty = 100;
        const int slotTieBreakWeight = 1;

        var objectiveTerms = new List<LinearExpr>();

        for (var t = 0; t < tasks.Count; t++)
        {
            var weights = tasks[t].AllowedAssignments
                .Select(a =>
                {
                    var lecturerCost = a.IsPrimary ? 0 : nonPrimaryLecturerPenalty;
                    var tieBreak = slotIndexByTimeSlotId[a.TimeSlotId] * slotTieBreakWeight + (int)a.Day;
                    return lecturerCost + tieBreak;
                })
                .ToArray();
            var min = weights.Min();
            var max = weights.Max();
            var cost = model.NewIntVar(min, max, $"task_cost_{t}");
            model.AddElement(assignmentVars[t], weights, cost);
            objectiveTerms.Add(cost);
        }

        var slotIdsByIndex = slotIndexByTimeSlotId
            .OrderBy(kv => kv.Value)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var programId in tasks.Select(t => t.StudyProgramId).Distinct())
        {
            for (var day = DayOfWeek.Monday; day <= DayOfWeek.Sunday; day++)
            {
                var occupied = new BoolVar[slotIdsByIndex.Count];
                for (var s = 0; s < slotIdsByIndex.Count; s++)
                {
                    var slotId = slotIdsByIndex[s];
                    var lits = new List<BoolVar>();
                    for (var t = 0; t < tasks.Count; t++)
                    {
                        if (tasks[t].StudyProgramId != programId)
                            continue;
                        if (taskSlotLiterals.TryGetValue((t, day, slotId), out var lit))
                            lits.Add(lit);
                    }

                    occupied[s] = model.NewBoolVar($"occ_p{programId}_d{(int)day}_s{s}");
                    if (lits.Count == 0)
                        model.Add(occupied[s] == 0);
                    else
                        LinkBoolOr(model, occupied[s], lits);
                }

                for (var s = 0; s < slotIdsByIndex.Count; s++)
                {
                    var before = model.NewBoolVar($"before_p{programId}_d{(int)day}_s{s}");
                    if (s == 0)
                        model.Add(before == 0);
                    else
                        LinkBoolOr(model, before, occupied.Take(s).ToList());

                    var after = model.NewBoolVar($"after_p{programId}_d{(int)day}_s{s}");
                    if (s == slotIdsByIndex.Count - 1)
                        model.Add(after == 0);
                    else
                        LinkBoolOr(model, after, occupied.Skip(s + 1).ToList());

                    var gap = model.NewBoolVar($"gap_p{programId}_d{(int)day}_s{s}");
                    model.AddBoolAnd([occupied[s].Not(), before, after]).OnlyEnforceIf(gap);
                    model.AddBoolOr([occupied[s], before.Not(), after.Not()]).OnlyEnforceIf(gap.Not());
                    objectiveTerms.Add(gap * gapWeight);
                }
            }
        }

        model.Minimize(LinearExpr.Sum(objectiveTerms));
    }
}

public readonly record struct ScheduledPlacement(
    int StudyProgramId,
    int SubjectId,
    int GroupIndex,
    int SessionIndex,
    int LecturerId,
    DayOfWeek Day,
    int TimeSlotId,
    string RoomNumber);

public sealed class ScheduleSolveResult
{
    public bool Success { get; init; }
    public CpSolverStatus? SolverStatus { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ScheduledPlacement> Placements { get; init; } = [];

    public static ScheduleSolveResult Succeeded(IReadOnlyList<ScheduledPlacement> placements) =>
        new() { Success = true, Placements = placements };

    public static ScheduleSolveResult Failed(CpSolverStatus status) =>
        new() { Success = false, SolverStatus = status };

    public static ScheduleSolveResult Infeasible(string? message = null) =>
        new() { Success = false, SolverStatus = CpSolverStatus.Infeasible, ErrorMessage = message };
}
