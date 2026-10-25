using System.Numerics;
using RayTracer.Materials;

namespace RayTracer.Hittables
{
    public class Sphere : IHittable
    {
        public Sphere(Vector3 center, float radius, IMaterial material)
        {
            Center = center;
            Radius = radius;
            Material = material;
        }

        public Vector3 Center { get; }

        public float Radius { get; }

        public IMaterial Material { get; }

        public Aabb BoundingBox => new(
            Center - new Vector3(MathF.Abs(Radius)),
            Center + new Vector3(MathF.Abs(Radius)));

        public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord record)
        {
            record = default;

            var oc = ray.Origin - Center;
            var a = ray.Direction.LengthSquared();
            var halfB = Vector3.Dot(oc, ray.Direction);
            var c = oc.LengthSquared() - Radius * Radius;

            var discriminant = halfB * halfB - a * c;
            if (discriminant < 0)
            {
                return false;
            }

            var sqrtD = MathF.Sqrt(discriminant);
            var root = (-halfB - sqrtD) / a;
            if (root < tMin || root > tMax)
            {
                root = (-halfB + sqrtD) / a;
                if (root < tMin || root > tMax)
                {
                    return false;
                }
            }

            record.T = root;
            record.Point = ray.At(root);
            record.SetFaceNormal(ray, (record.Point - Center) / Radius);
            record.Material = Material;
            return true;
        }
    }
}