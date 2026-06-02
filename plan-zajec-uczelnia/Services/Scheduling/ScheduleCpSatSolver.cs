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
            return ScheduleSolveResult.Infeasible("Brak dozwolonych przypisań dla co najmniej jednej sesji.");

        var model = new CpModel();
        var assignmentVars = new IntVar[tasks.Count];

        for (var t = 0; t < tasks.Count; t++)
            assignmentVars[t] = model.NewIntVar(0, tasks[t].AllowedAssignments.Count - 1, $"a_{t}");

        var picks = CreatePickLiterals(model, tasks, assignmentVars);
        AddAtMostOneResourceConstraints(model, tasks, picks);
        AddAdjacentSlotConstraints(model, tasks, assignmentVars, slotIndexByTimeSlotId);
        AddCompactnessObjective(model, tasks, assignmentVars, slotIndexByTimeSlotId);

        var solver = new CpSolver
        {
            StringParameters = $"max_time_in_seconds:{timeLimitSeconds}"
        };

        var status = solver.Solve(model);
        if (status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
            return ScheduleSolveResult.Infeasible("Nie znaleziono planu spełniającego ograniczenia.");

        var solution = new List<ScheduledPlacement>(tasks.Count);
        for (var t = 0; t < tasks.Count; t++)
        {
            var choice = tasks[t].AllowedAssignments[(int)solver.Value(assignmentVars[t])];
            solution.Add(new ScheduledPlacement(
                tasks[t].StudyProgramId,
                tasks[t].SubjectId,
                tasks[t].GroupIndex,
                tasks[t].SessionIndex,
                tasks[t].LecturerId,
                choice.Day,
                choice.TimeSlotId,
                choice.RoomNumber));
        }

        return ScheduleSolveResult.Succeeded(solution);
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

                var lectKey = (tasks[t].LecturerId, a.Day, a.TimeSlotId);
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

    private static void AddAdjacentSlotConstraints(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        IntVar[] assignmentVars,
        IReadOnlyDictionary<int, int> slotIndexByTimeSlotId)
    {
        for (var i = 0; i < tasks.Count; i++)
        {
            for (var j = i + 1; j < tasks.Count; j++)
            {
                if (tasks[i].StudyProgramId != tasks[j].StudyProgramId)
                    continue;

                var adjacentLiterals = new List<BoolVar>();
                for (var ai = 0; ai < tasks[i].AllowedAssignments.Count; ai++)
                {
                    var a = tasks[i].AllowedAssignments[ai];
                    if (!slotIndexByTimeSlotId.TryGetValue(a.TimeSlotId, out var idxA))
                        continue;

                    for (var aj = 0; aj < tasks[j].AllowedAssignments.Count; aj++)
                    {
                        var b = tasks[j].AllowedAssignments[aj];
                        if (a.Day != b.Day)
                            continue;
                        if (!slotIndexByTimeSlotId.TryGetValue(b.TimeSlotId, out var idxB))
                            continue;
                        if (Math.Abs(idxA - idxB) != 1)
                            continue;

                        var both = model.NewBoolVar($"adj_{i}_{ai}_{j}_{aj}");
                        var pickI = model.NewBoolVar($"adj_i_{i}_{ai}");
                        var pickJ = model.NewBoolVar($"adj_j_{j}_{aj}");
                        model.Add(assignmentVars[i] == ai).OnlyEnforceIf(pickI);
                        model.Add(assignmentVars[i] != ai).OnlyEnforceIf(pickI.Not());
                        model.Add(assignmentVars[j] == aj).OnlyEnforceIf(pickJ);
                        model.Add(assignmentVars[j] != aj).OnlyEnforceIf(pickJ.Not());
                        model.AddBoolAnd(new[] { pickI, pickJ }).OnlyEnforceIf(both);
                        model.AddBoolOr(new[] { pickI.Not(), pickJ.Not() }).OnlyEnforceIf(both.Not());
                        adjacentLiterals.Add(both);
                    }
                }

                if (adjacentLiterals.Count > 0)
                    model.AddAtMostOne(adjacentLiterals);
            }
        }
    }

    private static void AddCompactnessObjective(
        CpModel model,
        IReadOnlyList<PlacementTask> tasks,
        IntVar[] assignmentVars,
        IReadOnlyDictionary<int, int> slotIndexByTimeSlotId)
    {
        var costs = new List<LinearExpr>();
        for (var t = 0; t < tasks.Count; t++)
        {
            var weights = tasks[t].AllowedAssignments
                .Select(a => slotIndexByTimeSlotId[a.TimeSlotId] * 10 + (int)a.Day)
                .ToArray();
            var min = weights.Min();
            var max = weights.Max();
            var cost = model.NewIntVar(min, max, $"cost_{t}");
            model.AddElement(assignmentVars[t], weights, cost);
            costs.Add(cost);
        }

        model.Minimize(LinearExpr.Sum(costs));
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
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<ScheduledPlacement> Placements { get; init; } = [];

    public static ScheduleSolveResult Succeeded(IReadOnlyList<ScheduledPlacement> placements) =>
        new() { Success = true, Placements = placements };

    public static ScheduleSolveResult Infeasible(string message) =>
        new() { Success = false, ErrorMessage = message };
}
