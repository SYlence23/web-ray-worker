using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace RayTracer
{

    public static class VectorMath
    {
        public static float NextFloat(this Random rng, float min, float max) =>
            min + (max - min) * rng.NextSingle();

        public static Vector3 RandomVector(Random rng, float min, float max) =>
            new(rng.NextFloat(min, max), rng.NextFloat(min, max), rng.NextFloat(min, max));

        public static Vector3 RandomInUnitSphere(Random rng)
        {
            while (true)
            {
                var p = RandomVector(rng, -1, 1);
                if (p.LengthSquared() < 1)
                {
                    return p;
                }
            }
        }

        public static Vector3 RandomUnitVector(Random rng) => Vector3.Normalize(RandomInUnitSphere(rng));

        public static Vector3 RandomInUnitDisk(Random rng)
        {
            while (true)
            {
                var p = new Vector3(rng.NextFloat(-1, 1), rng.NextFloat(-1, 1), 0);
                if (p.LengthSquared() < 1)
                {
                    return p;
                }
            }
        }

        public static bool NearZero(Vector3 v)
        {
            const float s = 1e-8f;
            return MathF.Abs(v.X) < s && MathF.Abs(v.Y) < s && MathF.Abs(v.Z) < s;
        }

        public static Vector3 Refract(Vector3 uv, Vector3 n, float etaiOverEtat)
        {
            var cosTheta = MathF.Min(Vector3.Dot(-uv, n), 1f);
            var outPerp = etaiOverEtat * (uv + cosTheta * n);
            var outParallel = -MathF.Sqrt(MathF.Abs(1f - outPerp.LengthSquared())) * n;
            return outPerp + outParallel;
        }
    }

}
