using System.Numerics;
using RayTracer.Hittables;

namespace RayTracer.Materials
{
    public class Lambertian : IMaterial
    {
        private readonly Vector3 _albedo;

        public Lambertian(Vector3 albedo)
        {
            _albedo = albedo;
        }

        public bool Scatter(in Ray rayIn, in HitRecord hit, Random rng, out Vector3 attenuation, out Ray scattered)
        {
            var direction = hit.Normal + VectorMath.RandomUnitVector(rng);
            if (VectorMath.NearZero(direction))
            {
                direction = hit.Normal;
            }

            scattered = new Ray(hit.Point, direction);
            attenuation = _albedo;
            return true;
        }
    }
}
