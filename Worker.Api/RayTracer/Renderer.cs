using System.Numerics;
using RayTracer.Hittables;
using RayTracer.Scenes;

namespace RayTracer
{
    public static class Renderer
    {
        
        public static void Render(
            Scene scene,
            RenderSettings settings,
            ImageBuffer image,
            Action? onRowDone,
            CancellationToken cancellationToken)
        {
            var width = settings.Width;
            var height = settings.Height;
            var camera = new Camera(scene.Camera, (float)width / height);
            var scale = 1f / settings.SamplesPerPixel;

            var options = new ParallelOptions { CancellationToken = cancellationToken };
            Parallel.For(0, height, options, y =>
            {
                var rng = new Random(HashCode.Combine(settings.Seed, y));

                for (var x = 0; x < width; x++)
                {
                    var color = Vector3.Zero;
                    for (var s = 0; s < settings.SamplesPerPixel; s++)
                    {
                        var u = (x + rng.NextSingle()) / (width - 1);
                        var v = (height - 1 - y + rng.NextSingle()) / (height - 1);
                        color += RayColor(camera.GetRay(u, v, rng), scene.World, settings.MaxDepth, rng);
                    }

                    image.SetPixel(
                        x,
                        y,
                        ToByte(MathF.Sqrt(color.X * scale)),
                        ToByte(MathF.Sqrt(color.Y * scale)),
                        ToByte(MathF.Sqrt(color.Z * scale)));
                }

                onRowDone?.Invoke();
            });
        }

        private static Vector3 RayColor(Ray ray, IHittable world, int maxDepth, Random rng)
        {
            var attenuation = Vector3.One;

            for (var depth = 0; depth < maxDepth; depth++)
            {
                if (!world.Hit(ray, 0.001f, float.PositiveInfinity, out var hit))
                {
                    var t = 0.5f * (Vector3.Normalize(ray.Direction).Y + 1f);
                    var sky = (1f - t) * Vector3.One + t * new Vector3(0.5f, 0.7f, 1.0f);
                    return attenuation * sky;
                }

                if (!hit.Material.Scatter(ray, hit, rng, out var materialColor, out var scattered))
                {
                    return Vector3.Zero;
                }

                attenuation *= materialColor;
                ray = scattered;
            }

            return Vector3.Zero;
        }

        private static byte ToByte(float value) => (byte)(256 * Math.Clamp(value, 0f, 0.999f));
    }
}
