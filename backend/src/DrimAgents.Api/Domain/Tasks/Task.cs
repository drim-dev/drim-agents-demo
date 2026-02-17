namespace DrimAgents.Api.Domain.Tasks;

public class Task
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public TaskStage Stage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum TaskStage
{
    Design,
    Plan,
    Implementation,
    Review,
    Done
}
