namespace Worker.Api.Services
{
    public class TaskCleanupService : BackgroundService
    {
        private readonly TaskManager _taskManager;
        private readonly ILogger<TaskCleanupService> _logger;

        public TaskCleanupService(TaskManager taskManager, ILogger<TaskCleanupService> logger)
        {
            _taskManager = taskManager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var removed = _taskManager.RemoveExpired();
                if (removed > 0)
                {
                    _logger.LogInformation("Removed {Count} expired tasks", removed);
                }
            }
        }
    }
}
