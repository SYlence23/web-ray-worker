using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Worker.Api.Models;
using Worker.Api.Options;
using Worker.Api.Services;

namespace Worker.Api.Controllers
{
    [ApiController]
    [Route("tasks")]
    public class TasksController : ControllerBase
    {
        // TODO: take this list from the RayTracer scenes once they exist.
        private static readonly string[] KnownScenes = ["spheres", "glass", "random"];

        private readonly TaskManager _taskManager;
        private readonly WorkerOptions _options;

        public TasksController(TaskManager taskManager, IOptions<WorkerOptions> options)
        {
            _taskManager = taskManager;
            _options = options.Value;
        }

        [HttpPost]
        [ProducesResponseType<CreateTaskResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public IActionResult Create(RenderRequest request)
        {
            if (!KnownScenes.Contains(request.Scene))
            {
                return Problem($"Unknown scene '{request.Scene}'.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (request.Complexity > _options.MaxComplexity)
            {
                return Problem(
                    $"Complexity {request.Complexity} is over the limit of {_options.MaxComplexity}.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            var task = _taskManager.TryStart(request);
            if (task is null)
            {
                return Problem("Worker is at full capacity.", statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return CreatedAtAction(nameof(Get), new { id = task.Id }, new CreateTaskResponse(task.Id));
        }

        [HttpGet("{id:guid}")]
        public ActionResult<TaskStatusResponse> Get(Guid id)
        {
            var task = _taskManager.Get(id);
            if (task is null)
            {
                return NotFound();
            }

            return new TaskStatusResponse(task.Status, task.Progress, task.Error);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Cancel(Guid id)
        {
            return _taskManager.Cancel(id) ? NoContent() : NotFound();
        }
    }
}
