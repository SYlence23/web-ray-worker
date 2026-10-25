namespace Worker.Api.Options
{
    public class WorkerOptions
    {
        public const string SectionName = "Worker";

        public string Name { get; set; } = Environment.MachineName;

        public int Capacity { get; set; } = 2;

        public long MaxComplexity { get; set; } = 150_000_000;

        public int MaxRenderSeconds { get; set; } = 300;

        public int TaskTtlMinutes { get; set; } = 10;
    }
}
