using System.Numerics;
using RayTracer.Hittables;

namespace RayTracer.Materials
{
    public interface IMaterial
    {
        bool Scatter(in Ray rayIn, in HitRecord hit, Random rng, out Vector3 attenuation, out Ray scattered);
    }
}
