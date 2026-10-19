using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Worker.Api.Models;
using Worker.Api.Options;
using Worker.Api.Services;

namespace Worker.Api.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly TaskManager _taskManager;
        private readonly WorkerOptions _options;

        public HealthController(TaskManager taskManager, IOptions<WorkerOptions> options)
        {
            _taskManager = taskManager;
            _options = options.Value;
        }

        [HttpGet]
        public ActionResult<HealthResponse> Get()
        {
            return new HealthResponse(_options.Name, _taskManager.Running, _taskManager.Capacity);
        }
    }
}
