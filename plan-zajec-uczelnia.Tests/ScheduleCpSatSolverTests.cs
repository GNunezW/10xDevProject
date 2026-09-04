using System.Diagnostics;
using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services.Scheduling;

namespace plan_zajec_uczelnia.Tests;

public class ScheduleCpSatSolverTests
{
    private const int LecturerId = 7;
    private const string Room = "A101";

    [Fact]
    public void Four_groups_one_lecturer_with_distinct_slots_succeeds_without_collisions()
    {
        var slotIds = new[] { 1, 2, 3, 4 };
        var slotIndexByTimeSlotId = slotIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        var allowed = slotIds
            .Select(id => new ScheduleAssignment(LecturerId, DayOfWeek.Monday, id, Room, IsPrimary: true))
            .ToArray();

        var tasks = Enumerable.Range(1, 4)
            .Select(group => new PlacementTask
            {
                TaskIndex = group - 1,
                StudyProgramId = 1,
                Semester = 1,
                SubjectId = 10,
                GroupIndex = group,
                SessionIndex = 1,
                InstructionTypeId = 1,
                PrimaryLecturerId = LecturerId,
                SubjectName = "Algorytmy",
                StudyProgramName = "Informatyka",
                PrimaryLecturerName = "Kowalski",
                AllowedAssignments = allowed
            })
            .ToList();

        var solver = new ScheduleCpSatSolver();
        var sw = Stopwatch.StartNew();
        var result = solver.Solve(tasks, slotIndexByTimeSlotId, timeLimitSeconds: 5);
        sw.Stop();

        Assert.True(result.Success, $"Expected a feasible plan, got {result.SolverStatus}");
        Assert.Equal(4, result.Placements.Count);
        Assert.False(ScheduleCollisionOracle.HasLecturerOrRoomClash(result.Placements));
        Assert.All(result.Placements, p => Assert.Equal(LecturerId, p.LecturerId));
        Assert.All(result.Placements, p =>
            Assert.Contains(p.Day, ScheduleGridData.GetDaysForMode(StudyMode.FullTime)));
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30), $"Solve took {sw.Elapsed}");
    }

    [Fact]
    public void PartTime_shaped_domain_places_only_on_weekend()
    {
        var slotIds = new[] { 1, 2 };
        var slotIndexByTimeSlotId = slotIds
            .Select((id, index) => (id, index))
            .ToDictionary(x => x.id, x => x.index);

        var allowed = new[]
        {
            new ScheduleAssignment(LecturerId, DayOfWeek.Saturday, 1, Room, IsPrimary: true),
            new ScheduleAssignment(LecturerId, DayOfWeek.Sunday, 2, Room, IsPrimary: true)
        };

        var tasks = Enumerable.Range(1, 2)
            .Select(group => new PlacementTask
            {
                TaskIndex = group - 1,
                StudyProgramId = 2,
                Semester = 1,
                SubjectId = 20,
                GroupIndex = group,
                SessionIndex = 1,
                InstructionTypeId = 1,
                PrimaryLecturerId = LecturerId,
                SubjectName = "Laboratorium",
                StudyProgramName = "Chemia niestac",
                PrimaryLecturerName = "Kowalski",
                AllowedAssignments = allowed
            })
            .ToList();

        var result = new ScheduleCpSatSolver().Solve(tasks, slotIndexByTimeSlotId, timeLimitSeconds: 5);

        Assert.True(result.Success, $"Expected a feasible plan, got {result.SolverStatus}");
        Assert.All(result.Placements, p =>
            Assert.Contains(p.Day, ScheduleGridData.GetDaysForMode(StudyMode.PartTime)));
    }
}
