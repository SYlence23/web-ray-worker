namespace RayTracer.Hittables
{
    public interface IHittable
    {
        Aabb BoundingBox { get; }

        bool Hit(in Ray ray, float tMin, float tMax, out HitRecord record);
    }
}
