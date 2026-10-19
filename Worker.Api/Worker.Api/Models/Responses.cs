namespace Worker.Api.Models
{
    public record HealthResponse(string Name, int Running, int Capacity);

    public record CreateTaskResponse(Guid Id);

    public record TaskStatusResponse(RenderTaskStatus Status, double Progress, string? Error);
}
