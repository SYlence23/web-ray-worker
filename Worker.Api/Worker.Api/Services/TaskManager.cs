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

        // Returns null when the worker is full.
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

            // Render on a thread pool thread, so the request returns at once.
            _ = Task.Run(() => Run(task));
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
                // If the user cancelled, the status is already Cancelled and this does nothing.
                Finish(task, RenderTaskStatus.Failed, $"Render took longer than {_options.MaxRenderSeconds} s.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task {Id} failed", task.Id);
                Finish(task, RenderTaskStatus.Failed, ex.Message);
            }

            _logger.LogInformation("Task {Id} finished: {Status}", task.Id, task.Status);
        }

        // Only a running task can change its status, so the first outcome wins.
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
