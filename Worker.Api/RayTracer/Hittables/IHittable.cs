namespace RayTracer.Hittables
{
    public interface IHittable
    {
        bool Hit(in Ray ray, float tMin, float tMax, out HitRecord record);
    }
}