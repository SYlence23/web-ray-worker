namespace RayTracer.Hittables
{
    public class BvhNode : IHittable
    {
        private readonly IHittable _left;
        private readonly IHittable _right;

        public BvhNode(IReadOnlyList<IHittable> objects)
            : this(objects.ToArray(), 0, objects.Count)
        {
        }

        private BvhNode(IHittable[] objects, int start, int end)
        {
            var count = end - start;
            if (count <= 0)
            {
                throw new ArgumentException("A BVH needs at least one object.", nameof(objects));
            }

            if (count == 1)
            {
                _left = _right = objects[start];
            }
            else if (count == 2)
            {
                _left = objects[start];
                _right = objects[start + 1];
            }
            else
            {
                var bounds = objects[start].BoundingBox;
                for (var i = start + 1; i < end; i++)
                {
                    bounds = Aabb.Union(bounds, objects[i].BoundingBox);
                }

                var size = bounds.Max - bounds.Min;
                Func<IHittable, float> key = size.X >= size.Y && size.X >= size.Z
                    ? o => o.BoundingBox.Center.X
                    : size.Y >= size.Z
                        ? o => o.BoundingBox.Center.Y
                        : o => o.BoundingBox.Center.Z;

                Array.Sort(objects, start, count, Comparer<IHittable>.Create((a, b) => key(a).CompareTo(key(b))));

                var mid = start + count / 2;
                _left = new BvhNode(objects, start, mid);
                _right = new BvhNode(objects, mid, end);
            }

            BoundingBox = Aabb.Union(_left.BoundingBox, _right.BoundingBox);
        }

        public Aabb BoundingBox { get; }

        public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord record)
        {
            record = default;
            if (!BoundingBox.Hit(ray, tMin, tMax))
            {
                return false;
            }

            var hitLeft = _left.Hit(ray, tMin, tMax, out var leftRecord);
            var hitRight = _right.Hit(ray, tMin, hitLeft ? leftRecord.T : tMax, out var rightRecord);

            if (hitRight)
            {
                record = rightRecord;
            }
            else if (hitLeft)
            {
                record = leftRecord;
            }

            return hitLeft || hitRight;
        }
    }
}
