using RayTracer;

namespace Worker.Api.Models
{
    public class RenderTask
    {
        private int _rowsDone;

        public RenderTask(RenderRequest request, int seed)
        {
            Request = request;
            Seed = seed;
            Image = new ImageBuffer(request.Width, request.Height);
        }

        public Guid Id { get; } = Guid.NewGuid();

        public RenderRequest Request { get; }

        public int Seed { get; }

        public ImageBuffer Image { get; }

        public CancellationTokenSource Cancellation { get; } = new();

        public RenderTaskStatus Status { get; set; } = RenderTaskStatus.Running;

        public double Progress => Math.Round(100.0 * Volatile.Read(ref _rowsDone) / Request.Height, 1);

        public string? Error { get; set; }

        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        public DateTime? FinishedAt { get; set; }

        public void RowDone() => Interlocked.Increment(ref _rowsDone);
    }
}
