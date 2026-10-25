using System.Numerics;

namespace RayTracer.Hittables
{
    // Axis-aligned bounding box.
    public readonly struct Aabb
    {
        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Vector3 Min { get; }

        public Vector3 Max { get; }

        public Vector3 Center => (Min + Max) / 2;

        public static Aabb Union(Aabb a, Aabb b) => new(Vector3.Min(a.Min, b.Min), Vector3.Max(a.Max, b.Max));

        // Slab test: the ray hits the box if its entry and exit ranges on all three axes overlap.
        public bool Hit(in Ray ray, float tMin, float tMax)
        {
            var invDirection = Vector3.One / ray.Direction;
            var t0 = (Min - ray.Origin) * invDirection;
            var t1 = (Max - ray.Origin) * invDirection;
            var near = Vector3.Min(t0, t1);
            var far = Vector3.Max(t0, t1);

            var enter = MathF.Max(tMin, MathF.Max(near.X, MathF.Max(near.Y, near.Z)));
            var exit = MathF.Min(tMax, MathF.Min(far.X, MathF.Min(far.Y, far.Z)));
            return exit > enter;
        }
    }
}
