using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RayTracer;
using RayTracer.Scenes;
using Worker.Api.Models;
using Worker.Api.Options;

namespace Worker.Api.Services
{
    public class TaskManager
    {
        private readonly ConcurrentDictionary<Guid, RenderTask> _tasks = new();
        private readonly object _lock = new();
        private readonly WorkerOptions _options;
        private readonly ILogger<TaskManager> _logger;

        public TaskManager(IOptions<WorkerOptions> options, ILogger<TaskManager> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public int Running => _tasks.Values.Count(t => t.Status == RenderTaskStatus.Running);

        public int Capacity => _options.Capacity;

        public RenderTask? TryStart(RenderRequest request)
        {
            RenderTask task;
            lock (_lock)
            {
                if (Running >= Capacity)
                {
                    return null;
                }

                task = new RenderTask(request, request.Seed ?? Random.Shared.Next());
                _tasks[task.Id] = task;
            }

            _ = Task.Factory.StartNew(() => Run(task), TaskCreationOptions.LongRunning);
            return task;
        }

        public RenderTask? Get(Guid id) => _tasks.TryGetValue(id, out var task) ? task : null;

        public bool Cancel(Guid id)
        {
            if (!_tasks.TryGetValue(id, out var task))
            {
                return false;
            }

            Finish(task, RenderTaskStatus.Cancelled, null);
            task.Cancellation.Cancel();
            return true;
        }

        public int RemoveExpired()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-_options.TaskTtlMinutes);
            var removed = 0;

            foreach (var task in _tasks.Values)
            {
                if (task.FinishedAt < cutoff && _tasks.TryRemove(task.Id, out _))
                {
                    task.Cancellation.Dispose();
                    removed++;
                }
            }

            return removed;
        }

        private void Run(RenderTask task)
        {
            var request = task.Request;
            task.Cancellation.CancelAfter(TimeSpan.FromSeconds(_options.MaxRenderSeconds));

            try
            {
                var scene = SceneLibrary.Create(request.Scene, task.Seed);
                var settings = new RenderSettings(request.Width, request.Height, request.SamplesPerPixel, request.MaxDepth, task.Seed);

                _logger.LogInformation("Task {Id} started: {Scene} {Width}x{Height}, {Spp} spp",
                    task.Id, request.Scene, request.Width, request.Height, request.SamplesPerPixel);

                Renderer.Render(scene, settings, task.Image, task.RowDone, task.Cancellation.Token);
                Finish(task, RenderTaskStatus.Done, null);
            }
            catch (OperationCanceledException)
            {
                Finish(task, RenderTaskStatus.Failed, $"Render took longer than {_options.MaxRenderSeconds} s.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {Id} failed", task.Id);
                Finish(task, RenderTaskStatus.Failed, (ex.InnerException ?? ex).Message);
            }

            _logger.LogInformation("Task {Id} finished: {Status}", task.Id, task.Status);
        }

        private void Finish(RenderTask task, RenderTaskStatus status, string? error)
        {
            lock (_lock)
            {
                if (task.Status != RenderTaskStatus.Running)
                {
                    return;
                }

                task.Status = status;
                task.Error = error;
                task.FinishedAt = DateTime.UtcNow;
            }
        }
    }
}
