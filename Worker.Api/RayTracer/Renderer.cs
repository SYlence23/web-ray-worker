using System.Numerics;
using RayTracer.Hittables;
using RayTracer.Scenes;

namespace RayTracer
{
    public static class Renderer
    {
        // Rendering uses its own low-priority threads instead of Parallel.For: Parallel.For runs on
        // the thread pool, and a long render there leaves the web server no threads to answer requests.
        public static void Render(
            Scene scene,
            RenderSettings settings,
            ImageBuffer image,
            Action? onRowDone,
            CancellationToken cancellationToken)
        {
            var camera = new Camera(scene.Camera, (float)settings.Width / settings.Height);
            var world = new BvhNode(scene.World.Objects);
            var nextRow = -1;
            Exception? error = null;

            void Work()
            {
                try
                {
                    int y;
                    while ((y = Interlocked.Increment(ref nextRow)) < settings.Height)
                    {
                        if (cancellationToken.IsCancellationRequested || error is not null)
                        {
                            return;
                        }

                        RenderRow(y, world, camera, settings, image);
                        onRowDone?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref error, ex, null);
                }
            }

            var threads = new Thread[Environment.ProcessorCount];
            for (var i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(Work) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            if (error is not null)
            {
                throw new InvalidOperationException("Rendering failed.", error);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private static void RenderRow(int y, IHittable world, Camera camera, RenderSettings settings, ImageBuffer image)
        {
            var width = settings.Width;
            var height = settings.Height;
            var scale = 1f / settings.SamplesPerPixel;
            var rng = new Random(HashCode.Combine(settings.Seed, y));

            for (var x = 0; x < width; x++)
            {
                var color = Vector3.Zero;
                for (var s = 0; s < settings.SamplesPerPixel; s++)
                {
                    var u = (x + rng.NextSingle()) / (width - 1);
                    var v = (height - 1 - y + rng.NextSingle()) / (height - 1);
                    color += RayColor(camera.GetRay(u, v, rng), world, settings.MaxDepth, rng);
                }

                image.SetPixel(
                    x,
                    y,
                    ToByte(MathF.Sqrt(color.X * scale)),
                    ToByte(MathF.Sqrt(color.Y * scale)),
                    ToByte(MathF.Sqrt(color.Z * scale)));
            }
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
