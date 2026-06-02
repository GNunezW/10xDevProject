using System.ComponentModel.DataAnnotations;

namespace plan_zajec_uczelnia.Models;

public class ScheduleRun
{
    public int Id { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ScheduleRunStatus Status { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public int SolverTimeLimitSeconds { get; set; } = 60;

    public ICollection<ScheduledSession> ScheduledSessions { get; set; } = [];
}
