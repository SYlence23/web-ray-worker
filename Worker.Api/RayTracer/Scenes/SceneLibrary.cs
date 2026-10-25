using System.Numerics;
using RayTracer.Hittables;
using RayTracer.Materials;

namespace RayTracer.Scenes
{
    public static class SceneLibrary
    {
        private static readonly Dictionary<string, Func<int, Scene>> Builders = new()
        {
            ["spheres"] = _ => Spheres(),
            ["glass"] = _ => Glass(),
            ["random"] = Random,
        };

        public static IReadOnlyCollection<string> Names => Builders.Keys;

        public static bool Exists(string name) => Builders.ContainsKey(name);

        public static Scene Create(string name, int seed) =>
            Builders.TryGetValue(name, out var build)
                ? build(seed)
                : throw new ArgumentException($"Unknown scene '{name}'.", nameof(name));

        private static Scene Spheres()
        {
            var world = new HittableList();
            world.Add(new Sphere(new Vector3(0, -100.5f, -1), 100, new Lambertian(new Vector3(0.8f, 0.8f, 0.0f))));
            world.Add(new Sphere(new Vector3(0, 0, -1), 0.5f, new Lambertian(new Vector3(0.1f, 0.2f, 0.5f))));
            world.Add(new Sphere(new Vector3(-1, 0, -1), 0.5f, new Lambertian(new Vector3(0.7f, 0.2f, 0.2f))));
            world.Add(new Sphere(new Vector3(1, 0, -1), 0.5f, new Metal(new Vector3(0.8f, 0.6f, 0.2f), 0.3f)));

            return new Scene(world, new CameraSettings(new Vector3(0, 0.5f, 1.5f), new Vector3(0, 0, -1), 50));
        }

        private static Scene Glass()
        {
            var world = new HittableList();
            world.Add(new Sphere(new Vector3(0, -100.5f, -1), 100, new Lambertian(new Vector3(0.5f, 0.5f, 0.5f))));
            world.Add(new Sphere(new Vector3(0, 0, -1), 0.5f, new Dielectric(1.5f)));
            world.Add(new Sphere(new Vector3(-1, 0, -1), 0.5f, new Dielectric(1.5f)));
            world.Add(new Sphere(new Vector3(-1, 0, -1), -0.4f, new Dielectric(1.5f)));
            world.Add(new Sphere(new Vector3(1, 0, -1), 0.5f, new Metal(new Vector3(0.9f, 0.9f, 0.9f), 0.0f)));
            world.Add(new Sphere(new Vector3(0.3f, -0.35f, -0.4f), 0.15f, new Lambertian(new Vector3(0.9f, 0.3f, 0.1f))));

            return new Scene(world, new CameraSettings(new Vector3(-2, 1.2f, 1), new Vector3(0, 0, -1), 35, 0.05f));
        }

        private static Scene Random(int seed)
        {
            var rng = new Random(seed);
            var world = new HittableList();
            world.Add(new Sphere(new Vector3(0, -1000, 0), 1000, new Lambertian(new Vector3(0.5f, 0.5f, 0.5f))));

            for (var a = -11; a < 11; a++)
            {
                for (var b = -11; b < 11; b++)
                {
                    var center = new Vector3(a + 0.9f * rng.NextSingle(), 0.2f, b + 0.9f * rng.NextSingle());
                    if ((center - new Vector3(4, 0.2f, 0)).Length() <= 0.9f)
                    {
                        continue;
                    }

                    var choose = rng.NextSingle();
                    IMaterial material = choose switch
                    {
                        < 0.8f => new Lambertian(VectorMath.RandomVector(rng, 0, 1) * VectorMath.RandomVector(rng, 0, 1)),
                        < 0.95f => new Metal(VectorMath.RandomVector(rng, 0.5f, 1), rng.NextFloat(0, 0.5f)),
                        _ => new Dielectric(1.5f),
                    };
                    world.Add(new Sphere(center, 0.2f, material));
                }
            }

            world.Add(new Sphere(new Vector3(0, 1, 0), 1, new Dielectric(1.5f)));
            world.Add(new Sphere(new Vector3(-4, 1, 0), 1, new Lambertian(new Vector3(0.4f, 0.2f, 0.1f))));
            world.Add(new Sphere(new Vector3(4, 1, 0), 1, new Metal(new Vector3(0.7f, 0.6f, 0.5f), 0.0f)));

            return new Scene(world, new CameraSettings(new Vector3(13, 2, 3), Vector3.Zero, 20, 0.1f, 10));
        }
    }
}
