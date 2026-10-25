using System.Numerics;
using RayTracer.Hittables;

namespace RayTracer.Materials
{
    public class Dielectric : IMaterial
    {
        private readonly float _refractionIndex;

        public Dielectric(float refractionIndex)
        {
            _refractionIndex = refractionIndex;
        }

        public bool Scatter(in Ray rayIn, in HitRecord hit, Random rng, out Vector3 attenuation, out Ray scattered)
        {
            attenuation = Vector3.One;
            var ratio = hit.FrontFace ? 1f / _refractionIndex : _refractionIndex;

            var unitDirection = Vector3.Normalize(rayIn.Direction);
            var cosTheta = MathF.Min(Vector3.Dot(-unitDirection, hit.Normal), 1f);
            var sinTheta = MathF.Sqrt(1f - cosTheta * cosTheta);

            var cannotRefract = ratio * sinTheta > 1f;
            var direction = cannotRefract || Reflectance(cosTheta, ratio) > rng.NextSingle()
                ? Vector3.Reflect(unitDirection, hit.Normal)
                : VectorMath.Refract(unitDirection, hit.Normal, ratio);

            scattered = new Ray(hit.Point, direction);
            return true;
        }

        private static float Reflectance(float cosine, float ratio)
        {
            var r0 = (1 - ratio) / (1 + ratio);
            r0 *= r0;
            return r0 + (1 - r0) * MathF.Pow(1 - cosine, 5);
        }
    }
}
