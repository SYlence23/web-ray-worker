using System.Numerics;
using RayTracer.Hittables;

namespace RayTracer.Materials
{
    public class Metal : IMaterial
    {
        private readonly Vector3 _albedo;
        private readonly float _fuzz;

        public Metal(Vector3 albedo, float fuzz)
        {
            _albedo = albedo;
            _fuzz = Math.Clamp(fuzz, 0f, 1f);
        }

        public bool Scatter(in Ray rayIn, in HitRecord hit, Random rng, out Vector3 attenuation, out Ray scattered)
        {
            var reflected = Vector3.Reflect(Vector3.Normalize(rayIn.Direction), hit.Normal);
            scattered = new Ray(hit.Point, reflected + _fuzz * VectorMath.RandomInUnitSphere(rng));
            attenuation = _albedo;
            return Vector3.Dot(scattered.Direction, hit.Normal) > 0;
        }
    }
}
