using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Worker.Api.Models;
using Worker.Api.Options;

namespace Worker.Api.Services
{
    public class TaskManager
    {
        private readonly ConcurrentDictionary<Guid, RenderTask> _tasks = new();
        private readonly object _lock = new();
        private readonly WorkerOptions _options;

        public TaskManager(IOptions<WorkerOptions> options)
        {
            _options = options.Value;
        }

        public int Running => _tasks.Values.Count(t => t.Status == RenderTaskStatus.Running);

        public int Capacity => _options.Capacity;

        // Returns null when the worker is full.
        public RenderTask? TryStart(RenderRequest request)
        {
            lock (_lock)
            {
                if (Running >= Capacity)
                {
                    return null;
                }

                var task = new RenderTask(request);
                _tasks[task.Id] = task;

                // TODO: start the actual render on a background thread.
                return task;
            }
        }

        public RenderTask? Get(Guid id) => _tasks.TryGetValue(id, out var task) ? task : null;

        public bool Cancel(Guid id)
        {
            lock (_lock)
            {
                if (!_tasks.TryGetValue(id, out var task))
                {
                    return false;
                }

                if (task.Status == RenderTaskStatus.Running)
                {
                    task.Status = RenderTaskStatus.Cancelled;
                    task.FinishedAt = DateTime.UtcNow;
                }

                return true;
            }
        }
    }
}
