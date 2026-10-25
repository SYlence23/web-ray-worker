namespace RayTracer.Hittables
{
    public class HittableList : IHittable
    {
        private readonly List<IHittable> _objects = new();

        public void Add(IHittable obj) => _objects.Add(obj);

        public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord record)
        {
            record = default;
            var hitAnything = false;
            var closest = tMax;

            foreach (var obj in _objects)
            {
                if (obj.Hit(ray, tMin, closest, out var temp))
                {
                    hitAnything = true;
                    closest = temp.T;
                    record = temp;
                }
            }

            return hitAnything;
        }
    }
}