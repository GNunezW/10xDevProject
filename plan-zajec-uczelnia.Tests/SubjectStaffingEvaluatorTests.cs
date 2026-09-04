using plan_zajec_uczelnia.Models;
using plan_zajec_uczelnia.Services;

namespace plan_zajec_uczelnia.Tests;

public class SubjectStaffingEvaluatorTests
{
    private const int LecturerId = 1;
    private const string AcademicYear = "2025/2026";

    [Fact]
    public void One_lecturer_with_enough_weekly_slots_covers_four_groups()
    {
        var (subject, context) = BuildFixture(
            lecturerCount: 1,
            weekdaySlotCount: 4,
            weekendSlotCount: 0);

        var result = SubjectStaffingEvaluator.Evaluate(subject, studentCount: 120, context);

        Assert.Equal(4, result.GroupCount);
        Assert.Equal(1, result.AssignedLecturers);
        Assert.True(result.IsSufficient);
    }

    [Fact]
    public void One_lecturer_with_too_few_weekly_slots_does_not_cover_four_groups()
    {
        var (subject, context) = BuildFixture(
            lecturerCount: 1,
            weekdaySlotCount: 3,
            weekendSlotCount: 0);

        var result = SubjectStaffingEvaluator.Evaluate(subject, studentCount: 120, context);

        Assert.Equal(4, result.GroupCount);
        Assert.Equal(1, result.AssignedLecturers);
        Assert.False(result.IsSufficient);
    }

    [Fact]
    public void Zero_lecturers_is_insufficient_even_when_four_groups_are_needed()
    {
        var (subject, context) = BuildFixture(
            lecturerCount: 0,
            weekdaySlotCount: 0,
            weekendSlotCount: 0);

        var result = SubjectStaffingEvaluator.Evaluate(subject, studentCount: 120, context);

        Assert.Equal(4, result.GroupCount);
        Assert.Equal(0, result.AssignedLecturers);
        Assert.False(result.IsSufficient);
    }

    [Fact]
    public void FullTime_subject_does_not_count_weekend_availability_as_capacity()
    {
        var (subject, context) = BuildFixture(
            lecturerCount: 1,
            weekdaySlotCount: 0,
            weekendSlotCount: 8);

        var result = SubjectStaffingEvaluator.Evaluate(subject, studentCount: 120, context);

        Assert.Equal(4, result.GroupCount);
        Assert.False(result.IsSufficient);
    }

    /// <summary>
    /// NumberOfSessions = 1 so weekly slots per group is 1 regardless of teaching-week math.
    /// Four groups therefore need four weekday slots.
    /// </summary>
    private static (Subject Subject, SubjectStaffingContext Context) BuildFixture(
        int lecturerCount,
        int weekdaySlotCount,
        int weekendSlotCount)
    {
        var program = new StudyProgram
        {
            Id = 1,
            Name = "Informatyka",
            StudyMode = StudyMode.FullTime,
            AcademicYear = AcademicYear
        };

        var instructionType = new InstructionType
        {
            Id = 1,
            Name = "Ćwiczenia",
            MaxStudentsPerGroup = 30
        };

        var subject = new Subject
        {
            Id = 1,
            Name = "Algorytmy",
            StudyProgramId = program.Id,
            StudyProgram = program,
            Semester = 1,
            NumberOfSessions = 1,
            InstructionTypeId = instructionType.Id,
            InstructionType = instructionType,
            SubjectLecturers = []
        };

        var availability = new Dictionary<int, HashSet<(DayOfWeek Day, int TimeSlotId)>>();

        if (lecturerCount > 0)
        {
            subject.SubjectLecturers.Add(new SubjectLecturer
            {
                SubjectId = subject.Id,
                LecturerId = LecturerId,
                IsPrimary = true
            });

            var slots = new HashSet<(DayOfWeek Day, int TimeSlotId)>();
            for (var i = 0; i < weekdaySlotCount; i++)
                slots.Add((DayOfWeek.Monday, i + 1));
            for (var i = 0; i < weekendSlotCount; i++)
                slots.Add((DayOfWeek.Saturday, 100 + i));
            availability[LecturerId] = slots;
        }

        var context = new SubjectStaffingContext
        {
            AvailabilityByLecturer = availability,
            SemesterPeriods =
            [
                new SemesterPeriod
                {
                    AcademicYear = AcademicYear,
                    SemesterOrdinal = 1,
                    StartDate = new DateOnly(2025, 10, 1),
                    EndDate = new DateOnly(2026, 1, 31)
                }
            ],
            NonWorkingDays = []
        };

        return (subject, context);
    }
}
