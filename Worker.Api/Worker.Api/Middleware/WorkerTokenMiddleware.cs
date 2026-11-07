using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Worker.Api.Options;

namespace Worker.Api.Middleware
{
    public class WorkerTokenMiddleware
    {
        public const string HeaderName = "X-Worker-Token";

        private readonly RequestDelegate _next;
        private readonly byte[] _token;

        public WorkerTokenMiddleware(RequestDelegate next, IOptions<WorkerOptions> options)
        {
            _next = next;
            _token = Encoding.UTF8.GetBytes(options.Value.Token);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (_token.Length > 0 && !HasValidToken(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { title = "Missing or wrong worker token." });
                return;
            }

            await _next(context);
        }

        private bool HasValidToken(HttpRequest request)
        {
            var sent = Encoding.UTF8.GetBytes(request.Headers[HeaderName].ToString());
            return CryptographicOperations.FixedTimeEquals(sent, _token);
        }
    }
}
