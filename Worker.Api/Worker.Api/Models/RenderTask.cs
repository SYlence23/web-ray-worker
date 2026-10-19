namespace Worker.Api.Models
{
    public class RenderTask
    {
        public RenderTask(RenderRequest request)
        {
            Request = request;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public RenderRequest Request { get; }

        public RenderTaskStatus Status { get; set; } = RenderTaskStatus.Running;

        public double Progress { get; set; }

        public string? Error { get; set; }

        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        public DateTime? FinishedAt { get; set; }
    }
}
