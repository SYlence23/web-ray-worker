
using System.Text.Json;
using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Worker.Api.Options;
using Worker.Api.Services;

namespace Worker.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection(WorkerOptions.SectionName));
            builder.Services.AddSingleton<TaskManager>();
            builder.Services.AddHostedService<TaskCleanupService>();

            builder.Services.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
            builder.Services.AddOpenApi();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
